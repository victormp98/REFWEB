using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Categorias
        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Productos)
                .OrderByDescending(c => c.Activo)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
            return View(categorias);
        }

        // GET: Admin/Categorias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categorias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                categoria.FechaCreacion = DateTime.Now;
                categoria.Activo = true;
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Categoría \"{categoria.Nombre}\" creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // GET: Admin/Categorias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();
            return View(categoria);
        }

        // POST: Admin/Categorias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    categoria.FechaModificacion = DateTime.Now;
                    _context.Update(categoria);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Categoría \"{categoria.Nombre}\" actualizada.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Categorias.AnyAsync(c => c.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(categoria);
        }

        // POST: Admin/Categorias/Toggle/5  (soft delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null) return NotFound();

            categoria.Activo = !categoria.Activo;
            categoria.FechaModificacion = DateTime.Now;
            if (!categoria.Activo)
                categoria.FechaEliminacion = DateTime.Now;
            else
                categoria.FechaEliminacion = null;

            await _context.SaveChangesAsync();
            var estado = categoria.Activo ? "activada" : "desactivada";
            TempData["Success"] = $"Categoría \"{categoria.Nombre}\" {estado}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
