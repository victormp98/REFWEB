using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace RefWeb.Services
{
    public class VentasService : IVentasService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VentasService>? _logger;

        public VentasService(ApplicationDbContext context, IEmailService emailService, IHostEnvironment hostEnvironment, IConfiguration configuration, ILogger<VentasService>? logger = null)
        {
            _context = context;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Venta? Venta)> ProcesarVentaAsync(Venta venta, string userId)
        {
            // Si ya hay una transacción activa (ej: llamado desde ConfirmarPedido),
            // operamos dentro de ella sin abrir otra. El caller es responsable de commit/rollback.
            bool ownsTransaction = _context.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _context.Database.BeginTransactionAsync()
                : null;

            try
            {
                // 1. Validar Método de Pago
                if (!ValidarMetodoPago(venta.MetodoPago, venta.TipoVenta))
                {
                    return (false, $"El método de pago '{venta.MetodoPago}' no es válido para una venta de tipo '{venta.TipoVenta}'.", null);
                }

                // 2. Validar existencia, luego descontar stock de forma atómica (SEC-03 / RowVersion fix)
                // Se usa ExecuteUpdate con WHERE Stock >= cantidad para evitar race conditions.
                // Si filas afectadas == 0, el stock cambió entre la lectura y la escritura.
                foreach (var detalle in venta.VentasDetalle)
                {
                    // Leer estado actual para validar existencia y para el historial
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);

                    if (producto == null)
                        return (false, $"El producto con ID {detalle.ProductoId} no existe.", null);

                    if (producto.Stock < detalle.Cantidad)
                        return (false, $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, Solicitado: {detalle.Cantidad}.", null);

                    int stockAnterior = producto.Stock;
                    int stockNuevo    = stockAnterior - detalle.Cantidad;

                    // Actualización atómica: solo actualiza si el stock sigue siendo suficiente
                    int rowsAffected = await _context.Productos
                        .Where(p => p.Id == detalle.ProductoId && p.Stock >= detalle.Cantidad)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.Stock, stockNuevo)
                            .SetProperty(p => p.FechaUltimaVenta, DateTime.Now));

                    if (rowsAffected == 0)
                    {
                        // Otro proceso actualizó el stock entre nuestra lectura y escritura
                        return (false, $"Error de concurrencia en '{producto.Nombre}': el stock cambió. Por favor intenta de nuevo.", null);
                    }

                    // 3. Crear Movimiento de Inventario
                    var movimiento = new InventarioMovimiento
                    {
                        ProductoId     = producto.Id,
                        TipoMovimiento = "Salida",
                        Cantidad       = detalle.Cantidad,
                        StockAnterior  = stockAnterior,
                        StockNuevo     = stockNuevo,
                        TipoReferencia = "Venta",
                        UsuarioId      = userId,
                        Fecha          = DateTime.Now,
                        Notas          = $"Salida por venta folio {venta.Folio}",
                        EsCorreccion   = false
                    };
                    _context.InventarioMovimientos.Add(movimiento);

                    // 4. Verificar Stock Bajo y Notificar
                    if (stockNuevo <= producto.StockMinimo)
                    {
                        // Actualizar el objeto en memoria para la notificación
                        producto.Stock = stockNuevo;
                        _ = NotificarStockBajoAsync(producto);
                    }
                }

                // 4. Guardar Venta
                venta.UsuarioId = userId;
                venta.Fecha = DateTime.Now;
                venta.Estado = "Completada";
                venta.Folio = venta.Folio ?? $"V-{Guid.NewGuid():N}".Substring(0, 11); // Ej: V-a3f8c21b (único, sin colisión)
                
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                if (ownsTransaction && transaction != null)
                    await transaction.CommitAsync();

                return (true, "Venta procesada exitosamente.", venta);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (ownsTransaction && transaction != null) await transaction.RollbackAsync();
                return (false, "Error de concurrencia: El stock de uno o más productos ha cambiado. Por favor, intente de nuevo.", null);
            }
            catch (Exception ex)
            {
                if (ownsTransaction && transaction != null) await transaction.RollbackAsync();
                // No exponer detalles internos al cliente; loguear internamente
                var innerMsg = ex.InnerException?.Message ?? string.Empty;
                _logger?.LogError(ex, "Error al procesar venta. Inner: {Inner}", innerMsg);
                return (false, "Error al procesar la venta. Por favor contacta al administrador.", null);
            }
        }

        public async Task<(bool Success, string Message, Merma? Merma)> RegistrarMermaAsync(Merma merma, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FindAsync(merma.ProductoId);
                if (producto == null) return (false, "Producto no encontrado.", null);

                if (producto.Stock < merma.Cantidad)
                {
                    return (false, "No hay suficiente stock para reportar esta merma.", null);
                }

                int stockAnterior = producto.Stock;
                producto.Stock -= merma.Cantidad;

                // 1. Registrar Merma
                merma.ResponsableId = userId;
                merma.Fecha = DateTime.Now;
                _context.Mermas.Add(merma);

                // 2. Generar Movimiento de Inventario
                var movimiento = new InventarioMovimiento
                {
                    ProductoId = producto.Id,
                    TipoMovimiento = "Salida",
                    Cantidad = merma.Cantidad,
                    StockAnterior = stockAnterior,
                    StockNuevo = producto.Stock,
                    TipoReferencia = "Merma",
                    UsuarioId = userId,
                    Fecha = DateTime.Now,
                    Notas = $"Merma: {merma.TipoMerma} - {merma.Motivo}"
                };
                _context.InventarioMovimientos.Add(movimiento);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 3. Verificar Stock Bajo después de la merma
                if (producto.Stock <= producto.StockMinimo)
                {
                    _ = NotificarStockBajoAsync(producto);
                }

                return (true, "Merma registrada con éxito.", merma);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return (false, "Error de concurrencia al registrar la merma. El stock ha cambiado.", null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error al registrar merma: {ex.Message}", null);
            }
        }

        private async Task NotificarStockBajoAsync(Producto producto)
        {
            try
            {
                string templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "StockBajo.html");
                if (System.IO.File.Exists(templatePath))
                {
                    string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);
                    emailBody = emailBody.Replace("{Producto}", producto.Nombre)
                                       .Replace("{SKU}", producto.CodigoSKU ?? "N/A")
                                       .Replace("{StockActual}", producto.Stock.ToString())
                                       .Replace("{StockMinimo}", producto.StockMinimo.ToString())
                                       .Replace("{UrlAdmin}", _configuration["SiteUrl"] + "/Admin/Productos")
                                       .Replace("{Year}", DateTime.Now.Year.ToString());

                    var adminEmail = _configuration["Email:AdminEmail"] ?? "admin@refweb.com";
                    await _emailService.SendEmailAsync(adminEmail, $"⚠️ ALERTA: Stock Bajo - {producto.Nombre}", emailBody);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar notificación de stock bajo: " + ex.Message);
            }
        }

        public async Task<List<Venta>> ObtenerVentasPorCorteAsync(int corteCajaId)
        {
            return await _context.Ventas
                .Include(v => v.VentasDetalle)
                .ThenInclude(d => d.Producto)
                .Where(v => v.CorteCajaId == corteCajaId && v.Estado == "Completada")
                .ToListAsync();
        }

        public bool ValidarMetodoPago(string metodoPago, string tipoVenta)
        {
            if (string.IsNullOrEmpty(metodoPago) || string.IsNullOrEmpty(tipoVenta)) return false;

            if (tipoVenta == "Online")
            {
                // Solo tarjeta para ventas online
                return metodoPago == "Tarjeta";
            }
            else if (tipoVenta == "Mostrador")
            {
                // Efectivo o Tarjeta para ventas de mostrador (PDV)
                return metodoPago == "Efectivo" || metodoPago == "Tarjeta";
            }

            return false;
        }
    }
}
