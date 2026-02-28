using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class Pedido
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Folio { get; set; }

        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        public int DireccionEntregaId { get; set; }
        [ForeignKey("DireccionEntregaId")]
        public Direccion DireccionEntrega { get; set; }

        public DateTime FechaPedido { get; set; } = DateTime.Now;
        public DateTime? FechaPago { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; } = 0;

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuestos { get; set; }

        [Range(0.01, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [StringLength(30)]
        public string EstadoPedido { get; set; } = "Pendiente"; // Pendiente, Pagado, Enviado, Entregado, Cancelado

        [StringLength(50)]
        public string MetodoPago { get; set; } // Siempre 'Tarjeta' para online

        public int? VentaId { get; set; } // Una vez pagado, se genera la venta
        [ForeignKey("VentaId")]
        public Venta Venta { get; set; }

        [StringLength(500)]
        public string Notas { get; set; }

        // Relaciones
        public ICollection<PedidoDetalle> Detalles { get; set; }
        public Envio Envio { get; set; }
    }
}
