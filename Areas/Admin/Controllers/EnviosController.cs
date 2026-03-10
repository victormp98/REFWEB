using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Services;
using Microsoft.Extensions.Hosting;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class EnviosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<EnviosController> _logger;

        public EnviosController(ApplicationDbContext context, IEmailService emailService, IHostEnvironment hostEnvironment, ILogger<EnviosController> logger)
        {
            _context = context;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        // Listar pedidos que requieren gestión de envío
        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Envio)
                .Where(p => p.EstadoPedido == "Pagado"
                         || p.EstadoPedido == "En Proceso"
                         || p.EstadoPedido == "Enviado"
                         || p.EstadoPedido == "Entregado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidos);
        }

        // Cambiar de Pagado → En Proceso (Admin/Gerente prepara el paquete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarAEnProceso(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Envio)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null) return NotFound();

            if (pedido.EstadoPedido != "Pagado")
            {
                TempData["Error"] = "Solo los pedidos en estado 'Pagado' pueden pasar a 'En Proceso'.";
                return RedirectToAction(nameof(Index));
            }

            pedido.EstadoPedido = "En Proceso";
            await _context.SaveChangesAsync();

            _logger.LogInformation("[ENVIOS] Pedido #{PedidoId} movido a 'En Proceso'.", pedidoId);
            TempData["Success"] = $"Pedido #{pedido.Id.ToString("D6")} marcado como En Proceso.";
            return RedirectToAction(nameof(Index));
        }

        // Vista para asignar o editar guía
        public async Task<IActionResult> AsignarGuia(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Envio)
                .Include(p => p.DireccionEntrega)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null) return NotFound();

            if (pedido.Envio == null)
            {
                pedido.Envio = new Envio
                {
                    PedidoId = pedido.Id,
                    EstadoEnvio = "Preparando"
                };
            }

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarGuia(int pedidoId, string numeroGuia, string paqueteria, string notas)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Envio)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null) return NotFound();

            if (pedido.Envio == null)
            {
                var nuevoEnvio = new Envio
                {
                    PedidoId = pedido.Id,
                    NumeroGuia = numeroGuia,
                    Paqueteria = paqueteria,
                    Notas = notas,
                    FechaEnvio = DateTime.Now,
                    EstadoEnvio = "Enviado",
                    UrlRastreo = ""
                };
                _context.Envios.Add(nuevoEnvio);
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var guiaAnterior = pedido.Envio.NumeroGuia;
                bool guiaCambiada = pedido.EstadoPedido == "Enviado" && guiaAnterior != numeroGuia;

                pedido.Envio.NumeroGuia = numeroGuia;
                pedido.Envio.Paqueteria = paqueteria;
                pedido.Envio.Notas = notas;
                pedido.Envio.EstadoEnvio = "Enviado";
                pedido.Envio.UrlRastreo ??= "";
                if (pedido.Envio.FechaEnvio == null) pedido.Envio.FechaEnvio = DateTime.Now;
                _context.Update(pedido.Envio);

                // OPCIÓN B: si el pedido ya estaba enviado y cambiaron la guía, registrarlo silenciosamente
                if (guiaCambiada)
                {
                    _context.HistorialEnvios.Add(new HistorialEnvio
                    {
                        EnvioId = pedido.Envio.Id,
                        Estado = "Correcion",
                        Ubicacion = "Sistema Admin",
                        Descripcion = $"Guía corregida de '{guiaAnterior}' a '{numeroGuia}' por usuario {userId}",
                        Fecha = DateTime.Now
                    });
                    _logger.LogWarning("[ENVIOS] Guía del pedido #{PedidoId} corregida: '{Anterior}' → '{Nueva}' por {UserId}",
                        pedidoId, guiaAnterior, numeroGuia, userId);
                }
            }

            pedido.EstadoPedido = "Enviado";
            await _context.SaveChangesAsync();

            // Guardar en el historial de envíos (solo si es primera asignación)
            if (pedido.Envio!.Id > 0 && !_context.HistorialEnvios.Any(h => h.EnvioId == pedido.Envio.Id && h.Estado == "Enviado"))
            {
                _context.HistorialEnvios.Add(new HistorialEnvio
                {
                    EnvioId = pedido.Envio.Id,
                    Estado = "Enviado",
                    Ubicacion = "Centro de Distribución",
                    Descripcion = $"Paquetería: {paqueteria} — Guía: {numeroGuia}",
                    Fecha = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            // Enviar Correo de Pedido Enviado
            try
            {
                var pEnvio = await _context.Pedidos
                    .Include(p => p.Cliente)
                    .ThenInclude(c => c.Usuario)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pEnvio?.Cliente?.Usuario?.Email != null)
                {
                    string templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "PedidoEnviado.html");
                    if (System.IO.File.Exists(templatePath))
                    {
                        string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);
                        emailBody = emailBody.Replace("{Nombre}", pEnvio.Cliente.Nombre ?? "Cliente")
                                           .Replace("{PedidoId}", pEnvio.Folio)
                                           .Replace("{Guia}", numeroGuia)
                                           .Replace("{Year}", DateTime.Now.Year.ToString());
                        await _emailService.SendEmailAsync(pEnvio.Cliente.Usuario.Email, "¡Tu pedido va en camino! - RefWeb", emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ENVIOS] Error enviando correo de envío para pedido #{PedidoId}", pedidoId);
            }

            TempData["Success"] = "Guía de envío asignada correctamente.";
            return RedirectToAction(nameof(Index));
        }

    }
}
