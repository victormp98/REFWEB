using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RefWeb.Models;

namespace RefWeb.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets para nuestros modelos
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<CorteCaja> CortesCaja { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<VentaDetalle> VentasDetalle { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidosDetalle { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<HistorialEnvio> HistorialEnvios { get; set; }
        public DbSet<InventarioMovimiento> InventarioMovimientos { get; set; }
        public DbSet<Merma> Mermas { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<MetricaDiaria> MetricasDiarias { get; set; }
        public DbSet<ConfiguracionNegocio> ConfiguracionNegocio { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones y restricciones adicionales

            // Producto - Categoria
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta - CorteCaja
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.CorteCaja)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.CorteCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta - Usuario (vendedor)
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Venta - Cliente (opcional)
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // VentaDetalle - Venta
            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.Venta)
                .WithMany(v => v.VentasDetalle)
                .HasForeignKey(vd => vd.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            // VentaDetalle - Producto
            modelBuilder.Entity<VentaDetalle>()
                .HasOne(vd => vd.Producto)
                .WithMany(p => p.VentasDetalle)
                .HasForeignKey(vd => vd.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido - Cliente
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Pedidos)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido - DireccionEntrega
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.DireccionEntrega)
                .WithMany()
                .HasForeignKey(p => p.DireccionEntregaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pedido - Venta (uno a uno)
            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Venta)
                .WithOne()
                .HasForeignKey<Pedido>(p => p.VentaId)
                .OnDelete(DeleteBehavior.Restrict);

            // PedidoDetalle - Pedido
            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(pd => pd.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            // PedidoDetalle - Producto
            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(pd => pd.Producto)
                .WithMany(p => p.PedidosDetalle)
                .HasForeignKey(pd => pd.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Envio - Pedido (uno a uno)
            modelBuilder.Entity<Envio>()
                .HasOne(e => e.Pedido)
                .WithOne(p => p.Envio)
                .HasForeignKey<Envio>(e => e.PedidoId)
                .OnDelete(DeleteBehavior.Restrict);

            // HistorialEnvio - Envio
            modelBuilder.Entity<HistorialEnvio>()
                .HasOne(h => h.Envio)
                .WithMany(e => e.HistorialEnvios)
                .HasForeignKey(h => h.EnvioId)
                .OnDelete(DeleteBehavior.Cascade);

            // InventarioMovimiento - Producto
            modelBuilder.Entity<InventarioMovimiento>()
                .HasOne(im => im.Producto)
                .WithMany(p => p.InventarioMovimientos)
                .HasForeignKey(im => im.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventarioMovimiento - Usuario
            modelBuilder.Entity<InventarioMovimiento>()
                .HasOne(im => im.Usuario)
                .WithMany()
                .HasForeignKey(im => im.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // InventarioMovimiento - MovimientoOriginal
            modelBuilder.Entity<InventarioMovimiento>()
                .HasOne(im => im.MovimientoOriginal)
                .WithMany()
                .HasForeignKey(im => im.MovimientoOriginalId)
                .OnDelete(DeleteBehavior.Restrict);

            // Merma - Producto
            modelBuilder.Entity<Merma>()
                .HasOne(m => m.Producto)
                .WithMany(p => p.Mermas)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Merma - Responsable y AutorizadoPor
            modelBuilder.Entity<Merma>()
                .HasOne(m => m.Responsable)
                .WithMany()
                .HasForeignKey(m => m.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Merma>()
                .HasOne(m => m.AutorizadoPor)
                .WithMany()
                .HasForeignKey(m => m.AutorizadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Log - Usuario
            modelBuilder.Entity<Log>()
                .HasOne(l => l.Usuario)
                .WithMany()
                .HasForeignKey(l => l.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices configurados para MySQL (quitando [Activo] = 1 de HasFilter si da problemas, pero probemos)
            modelBuilder.Entity<Venta>()
                .HasIndex(v => v.Fecha)
                .HasDatabaseName("IX_Ventas_Fecha");

            modelBuilder.Entity<Venta>()
                .HasIndex(v => v.CorteCajaId)
                .HasDatabaseName("IX_Ventas_CorteCajaId");

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.CodigoBarras)
                .HasDatabaseName("IX_Productos_CodigoBarras");

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.CategoriaId)
                .HasDatabaseName("IX_Productos_CategoriaId");

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.ClienteId)
                .HasDatabaseName("IX_Pedidos_ClienteId");

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.EstadoPedido)
                .HasDatabaseName("IX_Pedidos_Estado");

            modelBuilder.Entity<Log>()
                .HasIndex(l => l.Fecha)
                .HasDatabaseName("IX_Logs_Fecha");

            modelBuilder.Entity<InventarioMovimiento>()
                .HasIndex(im => im.ProductoId)
                .HasDatabaseName("IX_InventarioMovimientos_ProductoId");
        }
    }
}
