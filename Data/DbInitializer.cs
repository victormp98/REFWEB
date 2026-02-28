using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RefWeb.Models;
using System.Threading.Tasks;

namespace RefWeb.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ── Roles ──────────────────────────────────────────────
            string[] roleNames = { "Admin", "Gerente", "Vendedor", "Cliente" };
            foreach (var roleName in roleNames)
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));

            // ── Usuarios de prueba ──────────────────────────────────
            await SeedUser(serviceProvider, userManager, "admin@refweb.com",    "Admin123!",    "Admin");
            await SeedUser(serviceProvider, userManager, "gerente@refweb.com",  "Gerente123!",  "Gerente");
            await SeedUser(serviceProvider, userManager, "vendedor@refweb.com", "Vendedor123!", "Vendedor");
            await SeedUser(serviceProvider, userManager, "cliente@refweb.com",  "Cliente123!",  "Cliente");

            // ── Categorías ─────────────────────────────────────────
            if (!await context.Categorias.AnyAsync())
            {
                var categorias = new[]
                {
                    new Categoria { Nombre = "Frenos",         Descripcion = "Pastillas, discos, tambores y cilindros de freno",     Activo = true, FechaCreacion = DateTime.Now },
                    new Categoria { Nombre = "Suspensión",     Descripcion = "Amortiguadores, muelles, rótulas y terminales",        Activo = true, FechaCreacion = DateTime.Now },
                    new Categoria { Nombre = "Motor",          Descripcion = "Filtros, juntas, correas y componentes de motor",       Activo = true, FechaCreacion = DateTime.Now },
                    new Categoria { Nombre = "Lubricantes",    Descripcion = "Aceites de motor, transmisión y líquidos en general",  Activo = true, FechaCreacion = DateTime.Now },
                    new Categoria { Nombre = "Eléctrico",      Descripcion = "Bujías, baterías, alternadores y sensores",            Activo = true, FechaCreacion = DateTime.Now },
                    new Categoria { Nombre = "Transmisión",    Descripcion = "Clutch, balatas, flechas y cajas de velocidades",      Activo = true, FechaCreacion = DateTime.Now },
                };
                context.Categorias.AddRange(categorias);
                await context.SaveChangesAsync();
            }

            // ── Productos de muestra ───────────────────────────────
            if (!await context.Productos.AnyAsync())
            {
                var cats = await context.Categorias.ToListAsync();
                int frenos      = cats.First(c => c.Nombre == "Frenos").Id;
                int suspension  = cats.First(c => c.Nombre == "Suspensión").Id;
                int motor       = cats.First(c => c.Nombre == "Motor").Id;
                int lubricantes = cats.First(c => c.Nombre == "Lubricantes").Id;
                int electrico   = cats.First(c => c.Nombre == "Eléctrico").Id;
                int transmision = cats.First(c => c.Nombre == "Transmisión").Id;

                var productos = new[]
                {
                    new Producto { Nombre = "Pastillas de Freno Delanteras",  CodigoSKU = "FRE-001", CodigoBarras = "7501234560001", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Pastillas de freno semimetálicas para la mayoría de sedanes.",         CategoriaId = frenos,      Precio = 249.00m, Stock = 25, StockMinimo = 5, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Disco de Freno Ventilado",        CodigoSKU = "FRE-002", CodigoBarras = "7501234560002", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Disco de freno ventilado de alto rendimiento.",                           CategoriaId = frenos,      Precio = 580.00m, Stock = 12, StockMinimo = 3, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Líquido de Frenos DOT4",          CodigoSKU = "FRE-003", CodigoBarras = "7501234560003", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Líquido de frenos DOT4 de alta temperatura. 500ml.",                       CategoriaId = frenos,      Precio = 89.00m,  Stock = 40, StockMinimo = 8, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Amortiguador Delantero KYB",      CodigoSKU = "SUS-001", CodigoBarras = "7501234560004", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Amortiguador de gas de tubo monotubular.",                                  CategoriaId = suspension,  Precio = 890.00m, Stock = 8,  StockMinimo = 2, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Rótula de Dirección",             CodigoSKU = "SUS-002", CodigoBarras = "7501234560005", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Rótula de dirección con funda protectora. Par delantero.",                  CategoriaId = suspension,  Precio = 320.00m, Stock = 15, StockMinimo = 4, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Filtro de Aceite Bosch",          CodigoSKU = "MOT-001", CodigoBarras = "7501234560006", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Filtro de aceite con válvula antirretorno.",                                CategoriaId = motor,       Precio = 95.00m,  Stock = 50, StockMinimo = 10, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Filtro de Aire",                  CodigoSKU = "MOT-002", CodigoBarras = "7501234560007", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Filtro de aire de alta eficiencia.",                                        CategoriaId = motor,       Precio = 145.00m, Stock = 30, StockMinimo = 6, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Correa de Distribución + Kit",    CodigoSKU = "MOT-003", CodigoBarras = "7501234560008", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Kit completo de correa de distribución con tensores.",                     CategoriaId = motor,       Precio = 1250.00m,Stock = 5,  StockMinimo = 2, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Aceite Motor 5W-30 Sintético",    CodigoSKU = "LUB-001", CodigoBarras = "7501234560009", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Aceite de motor 100% sintético 5W-30. 1 litro.",                            CategoriaId = lubricantes, Precio = 185.00m, Stock = 60, StockMinimo = 12, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Aceite ATF Dexron VI",            CodigoSKU = "LUB-002", CodigoBarras = "7501234560010", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Aceite para transmisión automática Dexron VI. 1 litro.",                   CategoriaId = lubricantes, Precio = 165.00m, Stock = 35, StockMinimo = 7, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Bujía NGK Iridio (x4)",           CodigoSKU = "ELE-001", CodigoBarras = "7501234560011", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Juego de 4 bujías de iridio NGK.",                                         CategoriaId = electrico,   Precio = 420.00m, Stock = 20, StockMinimo = 4, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Batería 12V 45Ah",                CodigoSKU = "ELE-002", CodigoBarras = "7501234560012", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Batería 12V 45Ah libre de mantenimiento. 18 meses de garantía.",            CategoriaId = electrico,   Precio = 1650.00m,Stock = 6,  StockMinimo = 2, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Sensor de Oxígeno (O2)",          CodigoSKU = "ELE-003", CodigoBarras = "7501234560013", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Sensor de oxígeno universal de 4 cables.",                                 CategoriaId = electrico,   Precio = 550.00m, Stock = 10, StockMinimo = 2, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Kit de Embrague Completo",        CodigoSKU = "TRA-001", CodigoBarras = "7501234560014", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Kit de embrague con disco, plato presor y collarín.",                     CategoriaId = transmision, Precio = 2100.00m,Stock = 4,  StockMinimo = 1, Activo = true, FechaCreacion = DateTime.Now },
                    new Producto { Nombre = "Flecha de Transmisión Derecha",   CodigoSKU = "TRA-002", CodigoBarras = "7501234560015", ImagenUrl = "", ImagenNombre = "", ImagenTipo = "", UbicacionAlmacen = "", Descripcion = "Flecha de transmisión lado derecho con juntas nuevas.",                   CategoriaId = transmision, Precio = 1480.00m,Stock = 3,  StockMinimo = 1, Activo = true, FechaCreacion = DateTime.Now },
                };
                context.Productos.AddRange(productos);
                await context.SaveChangesAsync();
            }
        }

        private static async Task SeedUser(IServiceProvider serviceProvider, UserManager<IdentityUser> userManager, string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            // Asegurar que tengan perfil de Cliente (nuevo o existente)
            if (user != null)
            {
                var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                if (!await context.Clientes.AnyAsync(c => c.UsuarioId == user.Id))
                {
                    try
                    {
                        var cliente = new Cliente
                        {
                            UsuarioId = user.Id,
                            Nombre = email.Split('@')[0],
                            Apellidos = "Usuario de Prueba", // Agregado: Campo requerido
                            Email = email,
                            Telefono = "555-0000",
                            FechaRegistro = DateTime.Now,
                            Activo = true
                        };
                        context.Clientes.Add(cliente);
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        var inner = ex.InnerException != null ? $"\nInner Error: {ex.InnerException.Message}" : "";
                        Console.WriteLine($"[ERROR SEEDING {email}]: {ex.Message}{inner}");
                        throw;
                    }
                }
            }
        }
    }
}

