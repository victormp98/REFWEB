using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using System.Security.Claims;

namespace RefWeb.Areas.Tienda.Controllers
{
    [Area("Tienda")]
    [Authorize(Roles = "Cliente")]
    public class CuentaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CuentaController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<Cliente> ObtenerO_CrearClienteAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);

            if (cliente == null)
            {
                cliente = new Cliente
                {
                    UsuarioId = userId,
                    Nombre = User.Identity.Name ?? "Cliente",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? "correo@ejemplo.com"
                };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }
            return cliente;
        }

        public async Task<IActionResult> LibretaDirecciones()
        {
            var cliente = await ObtenerO_CrearClienteAsync();
            var direcciones = await _context.Direcciones
                .Where(d => d.ClienteId == cliente.Id && d.Activo)
                .ToListAsync();

            return View(direcciones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaDireccion(Direccion direccion)
        {
            var cliente = await ObtenerO_CrearClienteAsync();
            
            direccion.ClienteId = cliente.Id;
            direccion.Activo = true;

            // Si es la primera, marcar como principal
            if (!await _context.Direcciones.AnyAsync(d => d.ClienteId == cliente.Id && d.Activo))
            {
                direccion.EsPrincipal = true;
            }

            _context.Direcciones.Add(direccion);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dirección agregada correctamente.";
            
            // Si venía del checkout, regresarlo al checkout
            var returnUrl = Request.Headers["Referer"].ToString();
            if (returnUrl.Contains("Checkout", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Checkout", "Tienda");

            return RedirectToAction(nameof(LibretaDirecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPrincipal(int id)
        {
            var cliente = await ObtenerO_CrearClienteAsync();
            var todas = await _context.Direcciones.Where(d => d.ClienteId == cliente.Id).ToListAsync();
            
            foreach (var dir in todas)
            {
                dir.EsPrincipal = (dir.Id == id);
            }
            
            await _context.SaveChangesAsync();
            TempData["Success"] = "Dirección principal actualizada.";
            return RedirectToAction(nameof(LibretaDirecciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDireccion(int id)
        {
            var cliente = await ObtenerO_CrearClienteAsync();
            var dir = await _context.Direcciones.FirstOrDefaultAsync(d => d.Id == id && d.ClienteId == cliente.Id);
            
            if (dir != null)
            {
                dir.Activo = false;
                dir.FechaEliminacion = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Info"] = "Dirección eliminada.";
            }

            return RedirectToAction(nameof(LibretaDirecciones));
        }
    }
}
