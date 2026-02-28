using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class PedidoDetalle
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Range(1, 1000)]
        public int Cantidad { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
    }
}
