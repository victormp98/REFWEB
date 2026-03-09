using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var hoy      = DateTime.Today;
            var semana   = hoy.AddDays(-6);
            var mes      = new DateTime(hoy.Year, hoy.Month, 1);

            // 4.4 FIX: Agrupar en BD con SumAsync/CountAsync en vez de cargar todas las filas a memoria
            var ventasHoyTotal = await _context.Ventas
                .Where(v => v.Fecha >= hoy && v.Estado == "Completada")
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            var ventasHoyCount = await _context.Ventas
                .CountAsync(v => v.Fecha >= hoy && v.Estado == "Completada");

            var ventasMesTotal = await _context.Ventas
                .Where(v => v.Fecha >= mes && v.Estado == "Completada")
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            var ventasMesCount = await _context.Ventas
                .CountAsync(v => v.Fecha >= mes && v.Estado == "Completada");

            // ── Inventario ──────────────────────────────────────────────
            var productosActivos   = await _context.Productos.CountAsync(p => p.Activo);
            var productosStockBajo = await _context.Productos
                .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .Include(p => p.Categoria)
                .OrderBy(p => p.Stock)
                .Take(5)
                .ToListAsync();

            // ── Pedidos ─────────────────────────────────────────────────
            var pedidosPendientes = await _context.Pedidos
                .CountAsync(p => p.EstadoPedido == "Pendiente" || p.EstadoPedido == "Procesando" || p.EstadoPedido == "Pagado");


            // ── Gráfica: ventas últimos 7 días ──────────────────────────
            var ventasSemana = await _context.Ventas
                .Where(v => v.Fecha >= semana && v.Estado == "Completada")
                .GroupBy(v => v.Fecha.Date)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(v => v.Total), Count = g.Count() })
                .OrderBy(g => g.Fecha)
                .ToListAsync();

            // Rellenar días sin ventas con 0
            var labels   = new List<string>();
            var totales  = new List<decimal>();
            var conteos  = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var dia = hoy.AddDays(-i);
                labels.Add(dia.ToString("ddd dd/MM"));
                var dato = ventasSemana.FirstOrDefault(v => v.Fecha == dia);
                totales.Add(dato?.Total ?? 0);
                conteos.Add(dato?.Count ?? 0);
            }

            // ── Gráfica: ventas por categoría (mes actual) ──────────────
            var ventasPorCategoria = await _context.VentasDetalle
                .Include(vd => vd.Venta)
                .Include(vd => vd.Producto).ThenInclude(p => p.Categoria)
                .Where(vd => vd.Venta.Fecha >= mes && vd.Venta.Estado == "Completada")
                .GroupBy(vd => vd.Producto.Categoria.Nombre)
                .Select(g => new { Categoria = g.Key, Total = g.Sum(vd => vd.Subtotal) })
                .OrderByDescending(g => g.Total)
                .Take(6)
                .ToListAsync();

            // ── Top 5 productos más vendidos ────────────────────────────
            var topProductos = await _context.VentasDetalle
                .Include(vd => vd.Venta)
                .Include(vd => vd.Producto)
                .Where(vd => vd.Venta.Fecha >= mes && vd.Venta.Estado == "Completada")
                .GroupBy(vd => new { vd.ProductoId, vd.Producto.Nombre })
                .Select(g => new { g.Key.Nombre, Cantidad = g.Sum(vd => vd.Cantidad), Total = g.Sum(vd => vd.Subtotal) })
                .OrderByDescending(g => g.Cantidad)
                .Take(5)
                .ToListAsync();

            // ── ViewBag ─────────────────────────────────────────────────
            ViewBag.VentasHoyTotal    = ventasHoyTotal;
            ViewBag.VentasHoyCount    = ventasHoyCount;
            ViewBag.VentasMesTotal    = ventasMesTotal;
            ViewBag.VentasMesCount    = ventasMesCount;
            ViewBag.ProductosActivos  = productosActivos;
            ViewBag.StockBajoCount    = productosStockBajo.Count;
            ViewBag.PedidosPendientes = pedidosPendientes;
            ViewBag.TotalUsuarios     = await _userManager.Users.CountAsync();

            ViewBag.GraficaLabels    = System.Text.Json.JsonSerializer.Serialize(labels);
            ViewBag.GraficaTotales   = System.Text.Json.JsonSerializer.Serialize(totales);
            ViewBag.GraficaConteos   = System.Text.Json.JsonSerializer.Serialize(conteos);
            ViewBag.GraficaCatLabels = System.Text.Json.JsonSerializer.Serialize(ventasPorCategoria.Select(v => v.Categoria).ToList());
            ViewBag.GraficaCatTotales = System.Text.Json.JsonSerializer.Serialize(ventasPorCategoria.Select(v => v.Total).ToList());

            ViewBag.ProductosStockBajo = productosStockBajo;
            ViewBag.TopProductos       = topProductos;

            return View();
        }
    }
}
