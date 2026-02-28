using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Services;

namespace RefWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Gerente")]
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IEmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;

        private const string ImageFolder = "images/productos";

        public ProductosController(
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment,
            IEmailService emailService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _emailService = emailService;
            _userManager = userManager;
        }

        // GET: Admin/Productos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Productos
                .Include(p => p.Categoria)
                .OrderByDescending(p => p.Id)
                .ToListAsync());
        }

        // GET: Admin/Productos/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.CategoriaId = new SelectList(await _context.Categorias.Where(c => c.Activo).ToListAsync(), "Id", "Nombre");
            return View();
        }

        // POST: Admin/Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile? imagen)
        {
            var camposIgnorar = new[] {
                "Categoria", "VentasDetalle", "PedidosDetalle", "InventarioMovimientos", "Mermas", "RowVersion",
                "ImagenUrl", "ImagenNombre", "ImagenTipo", "ImagenTamanio", "FechaImagen", "UbicacionAlmacen",
                "FechaUltimaCompra", "FechaUltimaVenta", "FechaModificacion", "FechaEliminacion"
            };
            foreach (var c in camposIgnorar) ModelState.Remove(c);

            if (imagen != null && imagen.Length > 0)
            {
                var (okVal, errorVal) = ValidarImagen(imagen);
                if (!okVal) ModelState.AddModelError("imagen", errorVal!);
            }

            if (ModelState.IsValid)
            {
                if (imagen != null && imagen.Length > 0)
                {
                    var (ok, error, url) = await GuardarImagen(imagen);
                    if (ok)
                    {
                        producto.ImagenUrl     = url!;
                        producto.ImagenNombre  = imagen.FileName;
                        producto.ImagenTipo    = imagen.ContentType;
                        producto.ImagenTamanio = (int)imagen.Length;
                        producto.FechaImagen   = DateTime.Now;
                    }
                }
                else
                {
                    producto.ImagenUrl = "";
                }

                producto.FechaCreacion = DateTime.Now;
                _context.Add(producto);
                await _context.SaveChangesAsync();

                await EnviarAlertaStockSiBajo(producto);

                TempData["Success"] = $"Producto \"{producto.Nombre}\" creado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            CapturarErroresModelState();
            ViewBag.CategoriaId = new SelectList(await _context.Categorias.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", producto.CategoriaId);
            return View(producto);
        }

        // GET: Admin/Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            ViewBag.Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync();
            return View(producto);
        }

        // POST: Admin/Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile? imagen)
        {
            if (id != producto.Id) return NotFound();

            var pDb = await _context.Productos.FindAsync(id);
            if (pDb == null) return NotFound();

            var camposIgnorar = new[] {
                "Categoria", "VentasDetalle", "PedidosDetalle", "InventarioMovimientos", "Mermas", "RowVersion",
                "ImagenUrl", "ImagenNombre", "ImagenTipo", "ImagenTamanio", "FechaImagen", "UbicacionAlmacen",
                "FechaUltimaCompra", "FechaUltimaVenta", "FechaModificacion", "FechaEliminacion"
            };
            foreach (var c in camposIgnorar) ModelState.Remove(c);

            if (imagen != null && imagen.Length > 0)
            {
                var (okVal, errorVal) = ValidarImagen(imagen);
                if (!okVal) ModelState.AddModelError("imagen", errorVal!);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    pDb.Nombre = producto.Nombre;
                    pDb.CodigoSKU = producto.CodigoSKU;
                    pDb.CodigoBarras = producto.CodigoBarras;
                    pDb.Descripcion = producto.Descripcion;
                    pDb.CategoriaId = producto.CategoriaId;
                    pDb.Precio = producto.Precio;
                    pDb.Stock = producto.Stock;
                    pDb.StockMinimo = producto.StockMinimo;
                    pDb.Activo = producto.Activo;
                    pDb.FechaModificacion = DateTime.Now;

                    if (imagen != null && imagen.Length > 0)
                    {
                        var (ok, error, url) = await GuardarImagen(imagen);
                        if (ok)
                        {
                            EliminarImagen(pDb.ImagenUrl);
                            pDb.ImagenUrl = url!;
                            pDb.ImagenNombre = imagen.FileName;
                            pDb.ImagenTipo = imagen.ContentType;
                            pDb.ImagenTamanio = (int)imagen.Length;
                            pDb.FechaImagen = DateTime.Now;
                        }
                    }

                    _context.Update(pDb);
                    await _context.SaveChangesAsync();

                    await EnviarAlertaStockSiBajo(pDb);

                    TempData["Success"] = $"Producto \"{pDb.Nombre}\" actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar en base de datos: " + ex.Message;
                }
            }

            CapturarErroresModelState();
            ViewBag.Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();

            producto.Activo = !producto.Activo;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Estado de \"{producto.Nombre}\" actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ────────────────────────────────────────────────────────

        private async Task EnviarAlertaStockSiBajo(Producto producto)
        {
            if (producto.Stock > producto.StockMinimo) return;

            try
            {
                // Obtener correos de los administradores
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "StockBajo.html");

                if (!System.IO.File.Exists(templatePath)) return;

                string template = await System.IO.File.ReadAllTextAsync(templatePath);
                string body = template
                    .Replace("{ProductoNombre}", producto.Nombre)
                    .Replace("{SKU}", producto.CodigoSKU ?? "-")
                    .Replace("{StockActual}", producto.Stock.ToString())
                    .Replace("{StockMinimo}", producto.StockMinimo.ToString())
                    .Replace("{Year}", DateTime.Now.Year.ToString());

                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                        await _emailService.SendEmailAsync(admin.Email, $"⚠️ Stock Bajo: {producto.Nombre}", body);
                }
            }
            catch (Exception ex)
            {
                // No detener el flujo principal si falla el correo
                Console.WriteLine($"[STOCK ALERT] Error: {ex.Message}");
            }
        }

        private (bool Ok, string? Error) ValidarImagen(IFormFile imagen)
        {
            var validExts = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imagen.FileName).ToLowerInvariant();
            if (!validExts.Contains(ext)) return (false, "Formato no permitido (usa JPG, PNG o WebP).");
            if (imagen.Length > 3 * 1024 * 1024) return (false, "La imagen es muy pesada (máx 3MB).");
            return (true, null);
        }

        private async Task<(bool Ok, string? Error, string? Url)> GuardarImagen(IFormFile imagen)
        {
            var folder = Path.Combine(_hostEnvironment.WebRootPath, ImageFolder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            return (true, null, $"/{ImageFolder}/{fileName}");
        }

        private void EliminarImagen(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, url.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }

        private void CapturarErroresModelState()
        {
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            if (errores.Any())
            {
                TempData["Error"] = "Faltan datos o son incorrectos: " + string.Join(" | ", errores);
            }
        }
    }
}
