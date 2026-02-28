using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using RefWeb.Services;
using Stripe;
using Newtonsoft.Json;
using System.Security.Claims;

namespace RefWeb.Areas.Tienda.Controllers
{
    [Area("Tienda")]
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVentasService _ventasService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IHostEnvironment _hostEnvironment;

        public TiendaController(ApplicationDbContext context, 
            IVentasService ventasService, 
            IConfiguration configuration,
            IEmailService emailService,
            IHostEnvironment hostEnvironment)
        {
            _context = context;
            _ventasService = ventasService;
            _configuration = configuration;
            _emailService = emailService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(int? categoriaId, int pagina = 1)
        {
            int itemsPorPagina = 12;
            var query = _context.Productos.Where(p => p.Activo);

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            var totalItems = await query.CountAsync();
            var productos = await query
                .OrderByDescending(p => p.Id)
                .Skip((pagina - 1) * itemsPorPagina)
                .Take(itemsPorPagina)
                .ToListAsync();

            ViewBag.Categorias = await _context.Categorias.Where(c => c.Activo).ToListAsync();
            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalItems / (double)itemsPorPagina);
            ViewBag.CategoriaSeleccionada = categoriaId;

            return View(productos);
        }

        public async Task<IActionResult> Producto(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

            if (producto == null) return NotFound();

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> AgregarCarrito(int productoId, int cantidad)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null || !producto.Activo)
            {
                return Json(new { success = false, message = "Producto no encontrado." });
            }

            if (producto.Stock < cantidad)
            {
                return Json(new { success = false, message = "Stock insuficiente." });
            }

            var carrito = GetCarrito();
            var item = carrito.FirstOrDefault(i => i.ProductoId == productoId);

            if (item != null)
            {
                item.Cantidad += cantidad;
            }
            else
            {
                carrito.Add(new PedidoDetalle
                {
                    ProductoId = productoId,
                    Producto = producto,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio
                });
            }

            SaveCarrito(carrito);
            return Json(new { success = true, count = carrito.Sum(i => i.Cantidad) });
        }

        [Authorize(Roles = "Cliente")]
        public IActionResult Carrito()
        {
            var carrito = GetCarrito();
            return View(carrito);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Checkout()
        {
            var carrito = GetCarrito();
            if (!carrito.Any()) return RedirectToAction(nameof(Carrito));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
            var direcciones = new List<Direccion>();

            if (cliente != null)
            {
                direcciones = await _context.Direcciones
                    .Where(d => d.ClienteId == cliente.Id && d.Activo)
                    .OrderByDescending(d => d.EsPrincipal)
                    .ToListAsync();
            }

            ViewBag.Direcciones = direcciones;
            ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];
            ViewBag.Total = carrito.Sum(i => i.Cantidad * i.PrecioUnitario);

            return View();
        }

        // BUG-A FIX: Página de detalle de producto
        public async Task<IActionResult> Detalle(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == id && p.Activo);

            if (producto == null) return NotFound();

            // Productos relacionados de la misma categoría (max 4, excluyendo el actual)
            var relacionados = await _context.Productos
                .Where(p => p.CategoriaId == producto.CategoriaId && p.Id != producto.Id && p.Activo)
                .Take(4)
                .ToListAsync();

            ViewBag.Relacionados = relacionados;
            return View(producto);
        }

        [HttpPost]
        [Authorize(Roles = "Cliente")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IniciarPago([FromBody] List<CartItemRequest>? items)
        {
            try
            {
                // Usar el carrito de la sesión del servidor (ya sincronizado por SyncCarrito)
                var sessionCart = GetCarrito();

                // Si la sesión no tiene datos pero vienen del body (localStorage), sincronizar ahora
                if ((!sessionCart.Any()) && items != null && items.Any())
                {
                    sessionCart = items.Select(i => new PedidoDetalle { ProductoId = i.ProductId, Cantidad = i.Cantidad }).ToList();
                    SaveCarrito(sessionCart);
                }

                if (!sessionCart.Any())
                {
                    return Json(new { error = "El carrito está vacío. Vuelve al carrito y agrega productos." });
                }

                // Obtener los IDs (filtrando ProductoId=0 que son inválidos)
                var productIds = sessionCart.Where(i => i.ProductoId > 0).Select(i => i.ProductoId).Distinct().ToList();
                if (!productIds.Any())
                {
                    return Json(new { error = "Carrito con productos inválidos. Por favor recarga la página e intenta de nuevo." });
                }

                var products = await _context.Productos
                    .Where(p => productIds.Contains(p.Id) && p.Activo)
                    .ToListAsync();

                decimal totalDecimal = 0;
                foreach (var item in sessionCart.Where(i => i.ProductoId > 0))
                {
                    var p = products.FirstOrDefault(x => x.Id == item.ProductoId);
                    if (p != null)
                    {
                        item.PrecioUnitario = p.Precio; // Siempre precio real del servidor
                        totalDecimal += p.Precio * item.Cantidad;
                    }
                }

                // Guardar el carrito con precios actualizados
                SaveCarrito(sessionCart);

                if (totalDecimal <= 0)
                {
                    return Json(new { error = "No se pudieron calcular los precios. Verifica que los productos estén disponibles." });
                }

                var total = (long)(totalDecimal * 100);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = total,
                    Currency = "mxn",
                    PaymentMethodTypes = new List<string> { "card" },
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return Json(new { clientSecret = intent.ClientSecret });
            }
            catch (StripeException ex)
            {
                return Json(new { error = "Error de Stripe: " + ex.StripeError?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al iniciar pago: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("Tienda/Webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], _configuration["Stripe:WebhookSecret"]);

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    // En un entorno real, aquí se usaría el PaymentIntent ID para buscar el pedido temporal y finalizarlo
                }

                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Cliente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarPedido(Direccion? direccionEnvio, int? DireccionExistenteId, string? paymentIntentId)
        {
            // ── SEGURIDAD: Verificar con Stripe que el pago realmente fue aprobado ─────
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                TempData["Error"] = "No se recibió confirmación de pago. Por favor intenta de nuevo.";
                return RedirectToAction(nameof(Carrito));
            }

            try
            {
                var piService = new PaymentIntentService();
                var intent = await piService.GetAsync(paymentIntentId);

                if (intent.Status != "succeeded")
                {
                    TempData["Error"] = $"El pago no fue aprobado por el procesador de pagos (estado: {intent.Status}). Por favor intenta de nuevo.";
                    return RedirectToAction(nameof(Checkout));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al verificar el pago con Stripe: " + ex.Message;
                return RedirectToAction(nameof(Checkout));
            }
            // ─────────────────────────────────────────────────────────────────────────
            var carrito = GetCarrito();
            if (!carrito.Any()) return RedirectToAction(nameof(Carrito));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
            
            if (cliente == null)
            {
                // Si el usuario no es cliente, crear perfil de cliente básico
                cliente = new Cliente { UsuarioId = userId, Nombre = User.Identity?.Name ?? "Cliente Web", Apellidos = string.Empty, Email = User.FindFirstValue(ClaimTypes.Email) ?? "correo@web.com" };
                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int finalDireccionId = 0;

                // 1. Resolver Dirección
                if (DireccionExistenteId.HasValue && DireccionExistenteId.Value > 0)
                {
                    var dirExistente = await _context.Direcciones.FirstOrDefaultAsync(d => d.Id == DireccionExistenteId.Value && d.ClienteId == cliente.Id);
                    if (dirExistente == null) throw new Exception("Dirección inválida.");
                    finalDireccionId = dirExistente.Id;
                }
                else if (direccionEnvio != null && !string.IsNullOrEmpty(direccionEnvio.Calle))
                {
                    direccionEnvio.ClienteId = cliente.Id;
                    direccionEnvio.TipoDireccion ??= "Casa";
                    direccionEnvio.Referencias   ??= "";
                    direccionEnvio.Colonia       ??= direccionEnvio.CodigoPostal ?? "";
                    direccionEnvio.EsPrincipal     = false;
                    _context.Direcciones.Add(direccionEnvio);
                    await _context.SaveChangesAsync();
                    finalDireccionId = direccionEnvio.Id;
                }
                else
                {
                    throw new Exception("Debe proveer una dirección de envío.");
                }

                // 2. Validar stock y releer precios desde la DB (MEJ-02 + MEJ-03)
                // Nunca confiar en los precios del cliente; siempre usar los de la base de datos.
                var productIds = carrito.Select(i => i.ProductoId).ToList();
                var productosDB = await _context.Productos
                    .Where(p => productIds.Contains(p.Id) && p.Activo)
                    .ToListAsync();

                foreach (var item in carrito)
                {
                    var prodDB = productosDB.FirstOrDefault(p => p.Id == item.ProductoId);
                    if (prodDB == null)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = $"El producto con ID {item.ProductoId} ya no está disponible.";
                        return RedirectToAction(nameof(Carrito));
                    }

                    // MEJ-03: Validar stock en el servidor
                    if (prodDB.Stock < item.Cantidad)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = $"Stock insuficiente para '{prodDB.Nombre}'. Disponible: {prodDB.Stock}, solicitado: {item.Cantidad}.";
                        return RedirectToAction(nameof(Carrito));
                    }

                    // MEJ-02: Sobrescribir precio con el valor real de la DB
                    item.PrecioUnitario = prodDB.Precio;
                }

                // 3. Crear Pedido
                var total = carrito.Sum(i => i.Cantidad * i.PrecioUnitario);
                var pedido = new Pedido
                {
                    Folio = $"WEB-{DateTime.Now:yyyyMMddHHmmss}",
                    ClienteId = cliente.Id,
                    DireccionEntregaId = finalDireccionId,
                    FechaPedido = DateTime.Now,
                    FechaPago = DateTime.Now,
                    Subtotal = total,
                    Total = total,
                    EstadoPedido = "Pagado",
                    MetodoPago = "Tarjeta",
                    Notas = ""
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 3b. Guardar los detalles del pedido (PedidoDetalle)
                var detallesPedido = carrito.Select(i => new PedidoDetalle
                {
                    PedidoId       = pedido.Id,
                    ProductoId     = i.ProductoId,
                    Cantidad       = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario
                }).ToList();
                _context.PedidosDetalle.AddRange(detallesPedido);
                await _context.SaveChangesAsync();


                // 3. Crear Venta (Deducción de Stock)
                var venta = new Venta
                {
                    Folio = pedido.Folio,
                    TipoVenta = "Online",
                    MetodoPago = "Tarjeta",
                    Subtotal = total,
                    Total = total,
                    ClienteId = cliente.Id,
                    VentasDetalle = carrito.Select(i => new VentaDetalle
                    {
                        ProductoId = i.ProductoId,
                        Cantidad = i.Cantidad,
                        PrecioUnitario = i.PrecioUnitario,
                        Subtotal = i.Cantidad * i.PrecioUnitario
                    }).ToList()
                };

                var (success, message, result) = await _ventasService.ProcesarVentaAsync(venta, userId);
                if (!success)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = message;
                    return RedirectToAction(nameof(Carrito));
                }

                // 4. Vincular Venta al Pedido
                pedido.VentaId = result.Id;

                // 4.1 Crear registro inicial de Envío y su Historial para poblar la línea de tiempo
                var envioInicial = new Envio
                {
                    PedidoId = pedido.Id,
                    EstadoEnvio = "Preparando",
                    UrlRastreo = "",
                    NumeroGuia = "",
                    Paqueteria = "",
                    Notas = "",
                    FechaEnvio = DateTime.Now // Fecha de inicio de preparación
                };
                _context.Envios.Add(envioInicial);
                await _context.SaveChangesAsync();

                _context.HistorialEnvios.Add(new HistorialEnvio
                {
                    EnvioId = envioInicial.Id,
                    Estado = "Preparando",
                    Ubicacion = "Tienda En Línea",
                    Descripcion = "",
                    Fecha = DateTime.Now
                });
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // 5. Enviar Correo de Confirmación
                try
                {
                    // BUG-04 FIX: Si el usuario escogió una dirección existente, direccionEnvio llega null.
                    // Cargamos la dirección de la DB para tener el objeto completo antes de usarlo en el email.
                    var direccionParaEmail = direccionEnvio;
                    if (direccionParaEmail == null && DireccionExistenteId.HasValue && DireccionExistenteId.Value > 0)
                    {
                        direccionParaEmail = await _context.Direcciones.FindAsync(DireccionExistenteId.Value);
                    }

                    string templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", "PedidoConfirmado.html");
                    if (System.IO.File.Exists(templatePath))
                    {
                        string emailBody = await System.IO.File.ReadAllTextAsync(templatePath);
                        string direccionTexto = direccionParaEmail != null
                            ? $"{direccionParaEmail.Calle}, {direccionParaEmail.Ciudad}"
                            : "Dirección no disponible";

                        emailBody = emailBody.Replace("{Nombre}", cliente.Nombre)
                                           .Replace("{PedidoId}", pedido.Folio)
                                           .Replace("{Total}", pedido.Total.ToString("C"))
                                           .Replace("{Fecha}", pedido.FechaPedido.ToString("dd/MM/yyyy HH:mm"))
                                           .Replace("{Direccion}", direccionTexto)
                                           .Replace("{Year}", DateTime.Now.Year.ToString());

                        await _emailService.SendEmailAsync(User.Identity.Name!, "Confirmación de Pedido - RefWeb", emailBody);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the order if email fails
                    Console.WriteLine("Error enviando correo de confirmación: " + ex.Message);
                }

                // 6. Limpiar Carrito
                HttpContext.Session.Remove("CarritoTienda");

                return RedirectToAction(nameof(Index), new { pago = "exito" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Mostrar detalle exacto del InnerException (nombre de columna, etc.)
                var detalle = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine("[ConfirmarPedido ERROR] " + ex.ToString());
                TempData["Error"] = "Error al procesar el pedido: " + detalle;
                return RedirectToAction(nameof(Carrito));
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCartItems([FromBody] List<CartItemRequest> items)
        {
            if (items == null || !items.Any()) return Json(new List<object>());

            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Productos
                .Where(p => productIds.Contains(p.Id) && p.Activo)
                .ToListAsync();

            var result = items.Select(item => {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                if (p == null) return null;
                return new {
                    productId = p.Id,
                    nombre = p.Nombre,
                    sku = p.CodigoSKU,
                    precio = p.Precio,
                    imagenUrl = p.ImagenUrl,
                    cantidad = item.Cantidad,
                    subtotal = p.Precio * item.Cantidad
                };
            }).Where(x => x != null).ToList();

            return Json(result);
        }

        [HttpPost]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> SyncCarrito([FromBody] List<CartItemRequest> items)
        {
            if (items == null || !items.Any()) 
            {
                HttpContext.Session.Remove("CarritoTienda");
                return Json(new { success = true });
            }

            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Productos
                .Where(p => productIds.Contains(p.Id) && p.Activo)
                .ToListAsync();

            var cart = items.Select(item => {
                var p = products.FirstOrDefault(x => x.Id == item.ProductId);
                if (p == null) return null;
                return new PedidoDetalle {
                    ProductoId = p.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = p.Precio
                };
            }).Where(x => x != null).ToList();

            SaveCarrito(cart);
            return Json(new { success = true });
        }

        public class CartItemRequest {
            public int ProductId { get; set; }
            public int Cantidad { get; set; }
        }
        private List<PedidoDetalle> GetCarrito()
        {
            var json = HttpContext.Session.GetString("CarritoTienda");
            if (string.IsNullOrEmpty(json)) return new List<PedidoDetalle>();
            return JsonConvert.DeserializeObject<List<PedidoDetalle>>(json) ?? new List<PedidoDetalle>();
        }

        private void SaveCarrito(List<PedidoDetalle> carrito)
        {
            var json = JsonConvert.SerializeObject(carrito, new JsonSerializerSettings 
            { 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore 
            });
            HttpContext.Session.SetString("CarritoTienda", json);
        }
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> MisPedidos(int pagina = 1)
        {
            const int itemsPorPagina = 8;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
            if (cliente == null) return NotFound();

            var query = _context.Pedidos
                .Where(p => p.ClienteId == cliente.Id)
                .OrderByDescending(p => p.FechaPedido);

            var totalPedidos = await query.CountAsync();

            var pedidos = await query
                .Skip((pagina - 1) * itemsPorPagina)
                .Take(itemsPorPagina)
                .Include(p => p.Envio)
                .ToListAsync();

            ViewBag.PaginaActual = pagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling(totalPedidos / (double)itemsPorPagina);

            return View(pedidos);
        }

        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> DetallePedido(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.UsuarioId == userId);
            if (cliente == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Include(p => p.DireccionEntrega)
                .Include(p => p.Envio)
                    .ThenInclude(e => e.HistorialEnvios)
                .FirstOrDefaultAsync(p => p.Id == id && p.ClienteId == cliente.Id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }
    }
}
