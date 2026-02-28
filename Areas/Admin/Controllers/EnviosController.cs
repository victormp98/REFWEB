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

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class EnviosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHostEnvironment _hostEnvironment;

        public EnviosController(ApplicationDbContext context, IEmailService emailService, IHostEnvironment hostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
        }

        // Listar pedidos pagados que necesitan envío o ya tienen uno
        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Envio)
                .Where(p => p.EstadoPedido == "Pagado" || p.EstadoPedido == "Enviado" || p.EstadoPedido == "Entregado")
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            return View(pedidos);
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
                pedido.Envio.NumeroGuia = numeroGuia;
                pedido.Envio.Paqueteria = paqueteria;
                pedido.Envio.Notas = notas;
                pedido.Envio.EstadoEnvio = "Enviado";
                pedido.Envio.UrlRastreo ??= "";
                if (pedido.Envio.FechaEnvio == null) pedido.Envio.FechaEnvio = DateTime.Now;
                _context.Update(pedido.Envio);
            }

            pedido.EstadoPedido = "Enviado";
            await _context.SaveChangesAsync();

            // Guardar en el historial de envíos
            _context.HistorialEnvios.Add(new HistorialEnvio
            {
                EnvioId = pedido.Envio.Id, // El Id ya existe después de SaveChangesAsync
                Estado = "Enviado",
                Ubicacion = "Centro de Distribución",
                Descripcion = "",
                Fecha = DateTime.Now
            });
            await _context.SaveChangesAsync();


            // Enviar Correo de Pedido Enviado
            try
            {
                // Volver a cargar el pedido con cliente y usuario
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
                Console.WriteLine("Error enviando correo de envío: " + ex.Message);
            }

            TempData["Success"] = "Guía de envío asignada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>MEJ-04: Marca el envío como Entregado y propaga el estado al Pedido.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EntregarPedido(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Envio)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null) return NotFound();

            // Actualizar estado del Envio
            if (pedido.Envio != null)
            {
                pedido.Envio.EstadoEnvio = "Entregado";
                pedido.Envio.FechaEntrega = DateTime.Now;
                _context.Update(pedido.Envio);
            }

            // Propagar estado al Pedido (MEJ-04)
            pedido.EstadoPedido = "Entregado";

            // Guardar en el historial de envíos
            _context.HistorialEnvios.Add(new HistorialEnvio
            {
                EnvioId = pedido.Envio.Id,
                Estado = "Entregado",
                Ubicacion = "Domicilio del Cliente",
                Descripcion = "",
                Fecha = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Pedido marcado como entregado correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
