using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class InventarioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InventarioController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Inventario
        public async Task<IActionResult> Index(int? productoId, string? tipo, int pagina = 1)
        {
            const int pageSize = 20;

            var query = _context.InventarioMovimientos
                .Include(m => m.Producto)
                .Include(m => m.Usuario)
                .AsQueryable();

            if (productoId.HasValue)
                query = query.Where(m => m.ProductoId == productoId);

            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(m => m.TipoMovimiento == tipo);

            var total = await query.CountAsync();
            var movimientos = await query
                .OrderByDescending(m => m.Fecha)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Productos     = await _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.FiltroProducto = productoId;
            ViewBag.FiltroTipo    = tipo;
            ViewBag.TotalPaginas  = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.PaginaActual  = pagina;

            return View(movimientos);
        }

        // GET: Admin/Inventario/Create
        public async Task<IActionResult> Create(int? productoId)
        {
            ViewBag.Productos = await _context.Productos
                .Where(p => p.Activo)
                .Include(p => p.Categoria)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            ViewBag.ProductoId = productoId;

            if (productoId.HasValue)
                ViewBag.StockActual = await _context.Productos
                    .Where(p => p.Id == productoId)
                    .Select(p => p.Stock)
                    .FirstOrDefaultAsync();

            return View();
        }

        // POST: Admin/Inventario/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productoId, string tipoMovimiento, int cantidad, string? notas)
        {
            if (cantidad <= 0)
            {
                TempData["Error"] = "La cantidad debe ser mayor a cero.";
                return RedirectToAction(nameof(Create), new { productoId });
            }

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return NotFound();

            int stockAnterior = producto.Stock;
            int stockNuevo;

            switch (tipoMovimiento)
            {
                case "Entrada":
                    stockNuevo = stockAnterior + cantidad;
                    break;
                case "Salida":
                    if (cantidad > stockAnterior)
                    {
                        TempData["Error"] = $"No hay suficiente stock. Stock actual: {stockAnterior}.";
                        return RedirectToAction(nameof(Create), new { productoId });
                    }
                    stockNuevo = stockAnterior - cantidad;
                    break;
                case "Ajuste":
                    stockNuevo = cantidad; // cantidad = nuevo stock absoluto
                    cantidad   = Math.Abs(stockNuevo - stockAnterior);
                    break;
                default:
                    TempData["Error"] = "Tipo de movimiento inválido.";
                    return RedirectToAction(nameof(Create), new { productoId });
            }

            var userId = _userManager.GetUserId(User);

            var movimiento = new InventarioMovimiento
            {
                ProductoId      = productoId,
                TipoMovimiento  = tipoMovimiento,
                Cantidad        = cantidad,
                StockAnterior   = stockAnterior,
                StockNuevo      = stockNuevo,
                TipoReferencia  = "AjusteManual",
                UsuarioId       = userId!,
                Fecha           = DateTime.Now,
                Notas           = notas ?? ""
            };

            producto.Stock = stockNuevo;

            _context.InventarioMovimientos.Add(movimiento);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Movimiento registrado. Stock de '{producto.Nombre}': {stockAnterior} → {stockNuevo}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Inventario/Producto/5
        public async Task<IActionResult> Producto(int id)
        {
            var producto = await _context.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null) return NotFound();

            var movimientos = await _context.InventarioMovimientos
                .Where(m => m.ProductoId == id)
                .Include(m => m.Usuario)
                .OrderByDescending(m => m.Fecha)
                .Take(50)
                .ToListAsync();

            ViewBag.Producto = producto;
            return View(movimientos);
        }
    }
}
