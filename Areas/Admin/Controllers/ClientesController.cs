using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Clientes
        public async Task<IActionResult> Index()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Usuario)
                .Include(c => c.Pedidos)
                .Include(c => c.Ventas) // Asumiendo que las Ventas Online/PDV se ligan a ClienteId
                .ToListAsync();

            var topClientes = clientes
                .Select(c => new
                {
                    Cliente = c,
                    TotalGastadoWEB = c.Pedidos?.Where(p => p.EstadoPedido != "Cancelado").Sum(p => p.Total) ?? 0,
                    TotalGastadoPDV = c.Ventas?.Where(v => v.Estado == "Completada" && v.TipoVenta == "Local").Sum(v => v.Total) ?? 0,
                    TicketsWeb = c.Pedidos?.Count(p => p.EstadoPedido != "Cancelado") ?? 0,
                    TicketsPDV = c.Ventas?.Count(v => v.Estado == "Completada" && v.TipoVenta == "Local") ?? 0
                })
                .Select(c => new
                {
                    c.Cliente,
                    TotalGlobal = c.TotalGastadoWEB + c.TotalGastadoPDV,
                    TotalTickets = c.TicketsWeb + c.TicketsPDV
                })
                .OrderByDescending(c => c.TotalGlobal)
                .ToList();

            // Pasaremos datos estructurados vía ViewBag para evitar ViewModel en este caso simple
            ViewBag.TopClientes = topClientes.Take(10).ToList();
            ViewBag.TodosClientes = topClientes;

            return View();
        }

        // GET: Admin/Clientes/Detalles/5
        public async Task<IActionResult> Detalles(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Usuario)
                .Include(c => c.Direcciones)   
                .Include(c => c.Pedidos)
                    .ThenInclude(p => p.Detalles)
                .Include(c => c.Ventas)
                    .ThenInclude(v => v.VentasDetalle)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
            {
                return NotFound();
            }

            return View(cliente);
        }
    }
}
