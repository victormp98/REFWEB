using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Models.ViewModels;
using RefWeb.Services;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExportService _exportService;

        public ReportesController(ApplicationDbContext context, ExportService exportService)
        {
            _context = context;
            _exportService = exportService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(VentasDiarias));
        }

        public async Task<IActionResult> VentasDiarias()
        {
            var hoy = DateTime.Today;
            var ventas = await _context.Ventas
                .Where(v => v.Fecha >= hoy && v.Estado == "Completada")
                .ToListAsync();

            ViewBag.TotalHoy = ventas.Sum(v => v.Total);
            ViewBag.ContadorHoy = ventas.Count;

            return View(ventas);
        }

        // ── EXPORTACIÓN ─────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> DescargarExcel(string? periodo = "hoy")
        {
            var ventas = await ObtenerVentasPorPeriodo(periodo);
            var bytes = _exportService.ExportarVentasExcel(ventas);
            var fileName = $"Ventas_{periodo}_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarPDF(string? periodo = "hoy")
        {
            var ventas = await ObtenerVentasPorPeriodo(periodo);
            var config = await _context.ConfiguracionNegocio.FirstOrDefaultAsync();
            var nombreNegocio = config?.Nombre ?? "RefWeb";
            var bytes = _exportService.ExportarVentasPDF(ventas, nombreNegocio);
            var fileName = $"Ventas_{periodo}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ── MERMAS ───────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> MermasTotales(string? periodo = "mes")
        {
            IQueryable<Merma> query = _context.Mermas
                .Include(m => m.Producto)
                .Include(m => m.Responsable);

            var hoy = DateTime.Today;
            var hace7Dias = hoy.AddDays(-7);
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAño = new DateTime(hoy.Year, 1, 1);

            query = periodo switch
            {
                "hoy" => query.Where(m => m.Fecha >= hoy),
                "semana" => query.Where(m => m.Fecha >= hace7Dias),
                "año" => query.Where(m => m.Fecha >= inicioAño),
                _ => query.Where(m => m.Fecha >= inicioMes)
            };

            var mermas = await query.OrderByDescending(m => m.Fecha).ToListAsync();

            ViewBag.TotalPerdido = mermas.Sum(m => m.Cantidad * (m.Producto?.Costo ?? 0));
            ViewBag.TotalArticulos = mermas.Sum(m => m.Cantidad);
            ViewBag.PeriodoActual = periodo;

            return View(mermas);
        }

        // ── RENDIMIENTO VENDEDORES ───────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> RendimientoVendedores(string? periodo = "mes")
        {
            IQueryable<Venta> query = _context.Ventas
                .Include(v => v.Usuario)
                .Where(v => v.Estado == "Completada" && v.UsuarioId != null);

            var hoy = DateTime.Today;
            var hace7Dias = hoy.AddDays(-7);
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAño = new DateTime(hoy.Year, 1, 1);

            query = periodo switch
            {
                "semana" => query.Where(v => v.Fecha >= hace7Dias),
                "mes" => query.Where(v => v.Fecha >= inicioMes),
                "año" => query.Where(v => v.Fecha >= inicioAño),
                _ => query.Where(v => v.Fecha >= inicioMes)
            };

            var ventas = await query.ToListAsync();

            var rendimiento = ventas
                .GroupBy(v => v.Usuario)
                .Select(g => new RendimientoVendedorVM
                {
                    Vendedor = g.Key?.UserName ?? "Usuario Eliminado",
                    Email = g.Key?.Email ?? "-",
                    Tickets = g.Count(),
                    TotalVendido = g.Sum(v => v.Total)
                })
                .OrderByDescending(x => x.TotalVendido)
                .ToList();

            ViewBag.PeriodoActual = periodo;
            return View(rendimiento);
        }

        // ── HELPERS ──────────────────────────────────────────────────────

        private async Task<List<Venta>> ObtenerVentasPorPeriodo(string? periodo)
        {
            IQueryable<Venta> query = _context.Ventas.Where(v => v.Estado == "Completada");

            var hoy = DateTime.Today;
            var hace7Dias = hoy.AddDays(-7);
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAño = new DateTime(hoy.Year, 1, 1);

            query = periodo switch
            {
                "semana" => query.Where(v => v.Fecha >= hace7Dias),
                "mes" => query.Where(v => v.Fecha >= inicioMes),
                "año" => query.Where(v => v.Fecha >= inicioAño),
                _ => query.Where(v => v.Fecha >= hoy)
            };

            return await query.OrderByDescending(v => v.Fecha).ToListAsync();
        }
    }
}
