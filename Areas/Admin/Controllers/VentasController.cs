using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class VentasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VentasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Auditoria()
        {
            var canceladas = await _context.Ventas
                .Include(v => v.Usuario) // Vendedor
                .Include(v => v.UsuarioCancela) // Quién canceló
                .Where(v => v.Estado == "Cancelada")
                .OrderByDescending(v => v.FechaCancelacion)
                .ToListAsync();

            return View(canceladas);
        }
    }
}
