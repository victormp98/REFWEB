using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ConfiguracionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ConfiguracionController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Admin/Configuracion
        public async Task<IActionResult> Index()
        {
            var config = await _context.ConfiguracionNegocio.FirstOrDefaultAsync();

            if (config == null)
            {
                // Crear registro inicial si no existe
                config = new ConfiguracionNegocio
                {
                    Nombre = "Mi Refaccionaria",
                    Telefono = "",
                    Email = "",
                    RFC = "",
                    Direccion = "",
                    LogoUrl = "",
                    LeyendaPie = "Gracias por su preferencia.",
                    ImpresoraPredeterminada = "",
                    Activo = true
                };
                _context.ConfiguracionNegocio.Add(config);
                await _context.SaveChangesAsync();
            }

            return View(config);
        }

        // POST: Admin/Configuracion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConfiguracionNegocio model, IFormFile? logo)
        {
            ModelState.Remove("LogoUrl");

            // Manejar logo
            if (logo != null && logo.Length > 0)
            {
                var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
                var validExts = new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp" };

                if (!validExts.Contains(ext))
                    ModelState.AddModelError("LogoUrl", "Solo se permiten imágenes JPG, PNG, SVG o WebP.");
                else if (logo.Length > 2 * 1024 * 1024)
                    ModelState.AddModelError("LogoUrl", "El logo no debe exceder 2MB.");
                else
                {
                    var folder = Path.Combine(_env.WebRootPath, "images", "negocio");
                    Directory.CreateDirectory(folder);
                    var fileName = $"logo{ext}";
                    var filePath = Path.Combine(folder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await logo.CopyToAsync(stream);
                    model.LogoUrl = $"/images/negocio/{fileName}";
                }
            }
            else
            {
                // Conservar logo existente
                var logoActual = await _context.ConfiguracionNegocio
                    .AsNoTracking()
                    .Where(c => c.Id == model.Id)
                    .Select(c => c.LogoUrl)
                    .FirstOrDefaultAsync();
                model.LogoUrl = logoActual ?? "";
            }

            if (ModelState.IsValid)
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Configuración guardada correctamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
