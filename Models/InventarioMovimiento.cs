using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class InventarioMovimiento
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [StringLength(20)]
        public string TipoMovimiento { get; set; } // 'Entrada', 'Salida', 'Ajuste'

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public int StockAnterior { get; set; }

        [Required]
        public int StockNuevo { get; set; }

        public int? ReferenciaId { get; set; } // ID de la venta, pedido, etc.
        [StringLength(20)]
        public string TipoReferencia { get; set; } // 'Venta', 'Pedido', 'Compra', 'Ajuste'

        public string UsuarioId { get; set; } // IdentityUser que realizó el movimiento
        [ForeignKey("UsuarioId")]
        public IdentityUser Usuario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string Notas { get; set; }

        public bool EsCorreccion { get; set; } = false;
        public int? MovimientoOriginalId { get; set; }
        [ForeignKey("MovimientoOriginalId")]
        public InventarioMovimiento MovimientoOriginal { get; set; }
    }
}
