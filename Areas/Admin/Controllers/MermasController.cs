using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Services;
using System.Security.Claims;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class MermasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVentasService _ventasService;

        public MermasController(ApplicationDbContext context, IVentasService ventasService)
        {
            _context = context;
            _ventasService = ventasService;
        }

        public async Task<IActionResult> Index()
        {
            var mermas = await _context.Mermas
                .Include(m => m.Producto)
                .Include(m => m.Responsable)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
            return View(mermas);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Productos = await _context.Productos.Where(p => p.Activo).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Merma merma)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Productos = await _context.Productos.Where(p => p.Activo).ToListAsync();
                return View(merma);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "Usuario no identificado. Por favor inicie sesión de nuevo.");
                ViewBag.Productos = await _context.Productos.Where(p => p.Activo).ToListAsync();
                return View(merma);
            }

            var (success, message, result) = await _ventasService.RegistrarMermaAsync(merma, userId);

            if (success)
            {
                TempData["Success"] = message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", message);
            TempData["Error"] = message;
            ViewBag.Productos = await _context.Productos.Where(p => p.Activo).ToListAsync();
            return View(merma);
        }
    }
}

