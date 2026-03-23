using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Services;
using System.Security.Claims;
using Newtonsoft.Json;
using Stripe;
using Microsoft.Extensions.Configuration;

namespace RefWeb.Areas.PDV.Controllers
{
    [Area("PDV")]
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVentasService _ventasService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public VentasController(ApplicationDbContext context, IVentasService ventasService, UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _ventasService = ventasService;
            _userManager = userManager;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // MEJ-06: Limpiar carrito al iniciar una nueva sesión de venta
            ClearCarrito();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarProducto(string? codigoBarras, int? id)
        {
            if (string.IsNullOrEmpty(codigoBarras) && (!id.HasValue || id.Value <= 0))
            {
                return Json(new { success = false, message = "Código de barras o producto no válido." });
            }

            Producto producto = null;

            // 1. Búsqueda exacta por ID (viene del Autocompletador)
            if (id.HasValue && id.Value > 0)
            {
                producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == id && p.Activo);
            }
            // 2. Búsqueda exacta por código
            else if (!string.IsNullOrEmpty(codigoBarras))
            {
                producto = await _context.Productos
                    .FirstOrDefaultAsync(p => 
                        (p.CodigoBarras == codigoBarras || p.CodigoSKU == codigoBarras)
                        && p.Activo);
                        
                // 3. Fallback a búsqueda exacta por nombre
                if (producto == null)
                {
                     producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.Nombre == codigoBarras && p.Activo);
                }
            }

            if (producto == null)
            {
                return Json(new { success = false, message = "Producto no encontrado." });
            }

            if (producto.Stock <= 0)
            {
                return Json(new { success = false, message = "Producto sin stock disponible." });
            }

            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(i => i.ProductoId == producto.Id);

            if (item != null)
            {
                if (producto.Stock <= item.Cantidad)
                {
                    return Json(new { success = false, message = "No hay más stock disponible para este producto." });
                }
                item.Cantidad++;
            }
            else
            {
                carrito.Add(new VentaDetalle
                {
                    ProductoId = producto.Id,
                    // Producto = producto, // Removido para evitar errores de serialización
                    Cantidad = 1,
                    PrecioUnitario = producto.Precio,
                    Subtotal = producto.Precio
                });
            }

            SaveCarrito(carrito);

            return Json(new { 
                success = true, 
                item = new { 
                    id = producto.Id, 
                    nombre = producto.Nombre, 
                    precio = producto.Precio, 
                    cantidad = carrito.First(i => i.ProductoId == producto.Id).Cantidad 
                },
                total = carrito.Sum(i => i.Cantidad * i.PrecioUnitario)
            });
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProductos(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var domainProductos = await _context.Productos
                .Where(p => p.Activo && (
                    p.Nombre.Contains(term) || 
                    (p.CodigoBarras != null && p.CodigoBarras.Contains(term)) || 
                    (p.CodigoSKU != null && p.CodigoSKU.Contains(term))
                ))
                .Take(10)
                .Select(p => new {
                    p.Id,
                    p.Nombre,
                    p.CodigoSKU,
                    p.CodigoBarras,
                    p.Precio
                })
                .ToListAsync();

            var productos = domainProductos.Select(p => new {
                label = p.Nombre + (!string.IsNullOrEmpty(p.CodigoSKU) ? " - " + p.CodigoSKU : (!string.IsNullOrEmpty(p.CodigoBarras) ? " - " + p.CodigoBarras : "")),
                value = p.CodigoSKU ?? p.CodigoBarras ?? p.Nombre,
                nombre = p.Nombre,
                id = p.Id,
                precio = p.Precio
            }).ToList();

            return Json(productos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarPagoPDV()
        {
            try
            {
                var carrito = GetCarrito();
                if (!carrito.Any()) return Json(new { success = false, message = "El carrito está vacío." });

                var totalDecimal = carrito.Sum(i => i.PrecioUnitario * i.Cantidad);
                if (totalDecimal <= 0) return Json(new { success = false, message = "Total inválido." });

                var secretKey = _configuration["Stripe:SecretKey"];
                if (string.IsNullOrEmpty(secretKey))
                    return Json(new { success = false, message = "Pago con tarjeta no configurado (falta Stripe:SecretKey)." });

                var total = (long)(totalDecimal * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = total,
                    Currency = "mxn",
                    PaymentMethodTypes = new List<string> { "card" },
                    Description = $"Venta PDV - {DateTime.Now:yyyy-MM-dd HH:mm}"
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return Json(new { success = true, clientSecret = intent.ClientSecret, publishableKey = _configuration["Stripe:PublishableKey"] });
            }
            catch (StripeException stripeEx)
            {
                return Json(new { success = false, message = $"Error de Stripe: {stripeEx.StripeError?.Message ?? stripeEx.Message}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al iniciar pago: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarVenta(string metodoPago, string notas, string? paymentIntentId = null)
        {
            var carrito = GetCarrito();
            if (!carrito.Any())
                return Json(new { success = false, message = "El carrito está vacío." });

            // 3.2 FIX: Verificar que haya un CorteCaja abierto antes de vender
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var corteActivo = await _context.CortesCaja
                .Where(c => c.Estado == "Abierto" && c.UsuarioAperturaId == userId)
                .FirstOrDefaultAsync();

            if (corteActivo == null)
                return Json(new { success = false, message = "⚠️ No tienes un turno de caja abierto. Ve a Corte de Caja y abre tu turno antes de vender." });

            // ── SEGURIDAD: Verificar con Stripe si el método de pago es Tarjeta ──────
            if (metodoPago == "Tarjeta")
            {
                if (string.IsNullOrEmpty(paymentIntentId))
                {
                    return Json(new { success = false, message = "No se recibió confirmación de pago con tarjeta." });
                }

                try
                {
                    var piService = new PaymentIntentService();
                    var intent = await piService.GetAsync(paymentIntentId);

                    if (intent.Status != "succeeded")
                    {
                        return Json(new { success = false, message = $"El pago no fue aprobado por Stripe (estado: {intent.Status})." });
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error al verificar el pago con Stripe: " + ex.Message });
                }
            }
            // ─────────────────────────────────────────────────────────────────────────

            // ── PRECIO FIX (SEC): Revalidar precios contra BD antes de procesar ────
            // El carrito PDV se almacena en sesión; un vendedor malintencionado podría
            // manipular los valores. Siempre usamos los precios reales de la BD.
            {
                var productIds = carrito.Select(i => i.ProductoId).Distinct().ToList();
                var productosDb = await _context.Productos
                    .Where(p => productIds.Contains(p.Id) && p.Activo)
                    .ToListAsync();

                foreach (var item in carrito)
                {
                    var prodDb = productosDb.FirstOrDefault(p => p.Id == item.ProductoId);
                    if (prodDb == null)
                        return Json(new { success = false, message = $"El producto con ID {item.ProductoId} ya no está disponible." });

                    item.PrecioUnitario = prodDb.Precio; // sobrescribir con precio real
                    item.Subtotal       = prodDb.Precio * item.Cantidad;
                }
            }
            // ─────────────────────────────────────────────────────────────────────────

            foreach(var item in carrito)
            {
                item.Producto = null; // Evitar error de entidad trackeada por EF
            }

            var venta = new Venta
            {
                TipoVenta    = "Mostrador",
                MetodoPago   = metodoPago,
                Notas        = notas,
                VentasDetalle = carrito,
                Subtotal     = carrito.Sum(i => i.PrecioUnitario * i.Cantidad),
                Impuestos    = 0,
                Total        = carrito.Sum(i => i.PrecioUnitario * i.Cantidad),
                UsuarioId    = userId,
                Estado       = "Completada",
                CorteCajaId  = corteActivo.Id  // 3.2 FIX: vincular al corte activo
            };

            var (success, message, resultVenta) = await _ventasService.ProcesarVentaAsync(venta, userId);

            if (success)
            {
                ClearCarrito();
                var user = await _userManager.GetUserAsync(User);
                return Json(new { success = true, message = "Venta finalizada con éxito.", folio = resultVenta?.Folio, vendedor = user?.UserName });
            }

            return Json(new { success = false, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProducto(int id)
        {
            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(i => i.ProductoId == id);
            
            if (item != null)
            {
                carrito.Remove(item);
                SaveCarrito(carrito);
                return Json(new { 
                    success = true, 
                    total = carrito.Sum(i => i.Cantidad * i.PrecioUnitario), 
                    itemsCount = carrito.Sum(i => i.Cantidad) 
                });
            }
            
            return Json(new { success = false, message = "Producto no encontrado en el carrito." });
        }

        [HttpGet]
        public async Task<IActionResult> ImprimirTicket(string folio)
        {
            var venta = await _context.Ventas
                .Include(v => v.VentasDetalle)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Folio == folio);

            if (venta == null) return NotFound();

            var ticketService = HttpContext.RequestServices.GetRequiredService<ITicketService>();
            var pdf = await ticketService.GenerarTicketVentaAsync(venta);

            return File(pdf, "application/pdf", $"Ticket_{folio}.pdf");
        }

        // ── CANCELACIÓN DE VENTA (solo Admin/Gerente) ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Gerente")]
        public async Task<IActionResult> CancelarVenta(int ventaId, string motivoCancelacion)
        {
            if (string.IsNullOrWhiteSpace(motivoCancelacion))
                return Json(new { success = false, message = "El motivo de cancelación es obligatorio." });

            var venta = await _context.Ventas
                .Include(v => v.VentasDetalle)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return Json(new { success = false, message = "Venta no encontrada." });

            if (venta.Estado == "Cancelada")
                return Json(new { success = false, message = "Esta venta ya fue cancelada." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Revertir stock de cada producto (operación atómica igual que en ventas)
                foreach (var detalle in venta.VentasDetalle)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
                    if (producto == null) continue;

                    int stockAnterior = producto.Stock;
                    int stockNuevo    = stockAnterior + detalle.Cantidad;

                    await _context.Productos
                        .Where(p => p.Id == detalle.ProductoId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, stockNuevo));

                    // Movimiento de inventario — Entrada por cancelación
                    _context.InventarioMovimientos.Add(new InventarioMovimiento
                    {
                        ProductoId     = detalle.ProductoId,
                        TipoMovimiento = "Entrada",
                        Cantidad       = detalle.Cantidad,
                        StockAnterior  = stockAnterior,
                        StockNuevo     = stockNuevo,
                        TipoReferencia = "CancelacionVenta",
                        UsuarioId      = userId,
                        Fecha          = DateTime.Now,
                        Notas          = $"Restitución por cancelación de venta {venta.Folio}. Motivo: {motivoCancelacion}",
                        EsCorreccion   = true
                    });
                }

                // Marcar la venta como cancelada
                venta.Estado             = "Cancelada";
                venta.FechaCancelacion   = DateTime.Now;
                venta.UsuarioCancelaId   = userId;
                venta.MotivoCancelacion  = motivoCancelacion;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Venta {venta.Folio} cancelada. Stock restituido correctamente." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Error al cancelar la venta: " + ex.Message });
            }
        }

        private List<VentaDetalle> GetCarrito()
        {
            var json = HttpContext.Session.GetString("CarritoPDV");
            if (string.IsNullOrEmpty(json)) return new List<VentaDetalle>();

            return JsonConvert.DeserializeObject<List<VentaDetalle>>(json) ?? new List<VentaDetalle>();
        }

        private void SaveCarrito(List<VentaDetalle> carrito)
        {
            var json = JsonConvert.SerializeObject(carrito, new JsonSerializerSettings 
            { 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore 
            });
            HttpContext.Session.SetString("CarritoPDV", json);
        }

        private void ClearCarrito()
        {
            HttpContext.Session.Remove("CarritoPDV");
        }
    }
}
