using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using System.Security.Claims;

namespace RefWeb.Areas.PDV.Controllers
{
    [Area("PDV")]
    [Authorize(Roles = "Admin,Gerente,Vendedor")]
    public class CorteCajaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CorteCajaController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: PDV/CorteCaja
        public async Task<IActionResult> Index()
        {
            var cortes = await _context.CortesCaja
                .Include(c => c.UsuarioApertura)
                .Include(c => c.UsuarioCierre)
                .OrderByDescending(c => c.FechaApertura)
                .Take(30)
                .ToListAsync();

            // Verificar si hay un corte abierto por el usuario actual
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.CorteActivo = await _context.CortesCaja
                .Where(c => c.Estado == "Abierto" && c.UsuarioAperturaId == userId)
                .FirstOrDefaultAsync();

            return View(cortes);
        }

        // POST: PDV/CorteCaja/Abrir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(decimal montoInicial, string? observaciones)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificar que no tenga ya un corte abierto
            var corteExistente = await _context.CortesCaja
                .Where(c => c.Estado == "Abierto" && c.UsuarioAperturaId == userId)
                .FirstOrDefaultAsync();

            if (corteExistente != null)
            {
                TempData["Error"] = "Ya tienes un turno abierto. Ciérralo antes de abrir uno nuevo.";
                return RedirectToAction(nameof(Index));
            }

            var corte = new CorteCaja
            {
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                UsuarioAperturaId = userId,
                Estado = "Abierto",
                Observaciones = observaciones
            };

            _context.CortesCaja.Add(corte);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Turno abierto con ${montoInicial:N2} en caja.";
            return RedirectToAction(nameof(Index));
        }

        // GET: PDV/CorteCaja/Cerrar/5
        public async Task<IActionResult> Cerrar(int id)
        {
            var corte = await _context.CortesCaja
                .Include(c => c.Ventas)
                .Include(c => c.UsuarioApertura)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (corte == null || corte.Estado != "Abierto") return NotFound();

            // Calcular totales del turno
            var ventas = await _context.Ventas
                .Where(v => v.CorteCajaId == id && v.Estado == "Completada")
                .ToListAsync();

            ViewBag.TotalEfectivo = ventas.Where(v => v.MetodoPago == "Efectivo").Sum(v => v.Total);
            ViewBag.TotalTarjeta = ventas.Where(v => v.MetodoPago == "Tarjeta").Sum(v => v.Total);
            ViewBag.TotalVentas = ventas.Sum(v => v.Total);
            ViewBag.NumeroVentas = ventas.Count;

            return View(corte);
        }

        // POST: PDV/CorteCaja/Cerrar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id, decimal montoFinal, string? observaciones)
        {
            var corte = await _context.CortesCaja.FindAsync(id);
            if (corte == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ventas = await _context.Ventas
                .Where(v => v.CorteCajaId == id && v.Estado == "Completada")
                .ToListAsync();

            corte.FechaCierre = DateTime.Now;
            corte.MontoFinal = montoFinal;
            corte.TotalVentas = ventas.Sum(v => v.Total);
            corte.UsuarioCierreId = userId;
            corte.Estado = "Cerrado";
            corte.Observaciones = string.IsNullOrEmpty(observaciones)
                ? corte.Observaciones
                : $"{corte.Observaciones} | Cierre: {observaciones}";

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Turno cerrado correctamente. Total vendido: {corte.TotalVentas:C}";
            return RedirectToAction(nameof(Index));
        }
    }
}
