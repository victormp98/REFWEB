using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class Producto
    {
        public Producto()
        {
            ImagenUrl = string.Empty;
            ImagenNombre = string.Empty;
            ImagenTipo = string.Empty;
            UnidadMedida = "Pieza";
            UbicacionAlmacen = string.Empty;
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código de barras es obligatorio.")]
        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required(ErrorMessage = "El SKU es obligatorio para el inventario.")]
        [StringLength(50)]
        public string? CodigoSKU { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "La descripción del producto es necesaria.")]
        public string? Descripcion { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Costo { get; set; }

        [Range(0, 1000000, ErrorMessage = "El stock debe ser un valor válido.")]
        public int Stock { get; set; } = 0;

        [Range(0, 1000, ErrorMessage = "El stock mínimo debe ser un valor válido.")]
        public int StockMinimo { get; set; } = 5;

        // Relación con Categoría
        public int CategoriaId { get; set; }
        [ForeignKey("CategoriaId")]
        public Categoria Categoria { get; set; }

        [Range(0, 100, ErrorMessage = "El impuesto debe estar entre 0 y 100.")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Impuesto { get; set; } = 16;

        [StringLength(20)]
        public string UnidadMedida { get; set; } = "Pieza";

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Peso { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Alto { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Ancho { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Profundidad { get; set; }

        // Imágenes
        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        [StringLength(100)]
        public string? ImagenNombre { get; set; }

        [StringLength(50)]
        public string? ImagenTipo { get; set; }

        public int? ImagenTamanio { get; set; }
        public DateTime? FechaImagen { get; set; }

        // Proveedor (por ahora solo ID, luego podríamos tener tabla Proveedores)
        public int? ProveedorId { get; set; }

        [StringLength(50)]
        public string? UbicacionAlmacen { get; set; }

        public DateTime? FechaUltimaCompra { get; set; }
        public DateTime? FechaUltimaVenta { get; set; }

        // Soft Delete
        public bool Activo { get; set; } = true;

        // Concurrencia
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }

        // Relaciones inversas
        public ICollection<VentaDetalle> VentasDetalle { get; set; }
        public ICollection<PedidoDetalle> PedidosDetalle { get; set; }
        public ICollection<InventarioMovimiento> InventarioMovimientos { get; set; }
        public ICollection<Merma> Mermas { get; set; }
    }
}
