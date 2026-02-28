using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Folio { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? CorteCajaId { get; set; }
        [ForeignKey("CorteCajaId")]
        public CorteCaja CorteCaja { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Impuestos { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [StringLength(20)]
        public string TipoVenta { get; set; } // 'Mostrador', 'Online'

        [StringLength(50)]
        public string MetodoPago { get; set; } // 'Efectivo', 'Tarjeta', etc.

        public string UsuarioId { get; set; } // Vendedor (Identity)
        [ForeignKey("UsuarioId")]
        public IdentityUser Usuario { get; set; }

        public int? ClienteId { get; set; } // Para ventas online (opcional)
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Completada"; // 'Completada', 'Cancelada'

        [StringLength(500)]
        public string? Notas { get; set; }

        // Cancelación
        public DateTime? FechaCancelacion { get; set; }
        public string? UsuarioCancelaId { get; set; }
        [ForeignKey("UsuarioCancelaId")]
        public IdentityUser? UsuarioCancela { get; set; }
        [StringLength(500)]
        public string? MotivoCancelacion { get; set; }

        // Relación con detalles
        public ICollection<VentaDetalle> VentasDetalle { get; set; }
    }
}
