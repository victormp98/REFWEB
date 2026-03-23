using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class CorteCaja
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public DateTime? FechaCierre { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontoFinal { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalVentas { get; set; }

        // Usuarios (Identity)
        public string UsuarioAperturaId { get; set; }
        [ForeignKey("UsuarioAperturaId")]
        public IdentityUser UsuarioApertura { get; set; }

        public string? UsuarioCierreId { get; set; }
        [ForeignKey("UsuarioCierreId")]
        public IdentityUser? UsuarioCierre { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "Abierto"; // 'Abierto', 'Cerrado'

        [StringLength(500)]
        public string? Observaciones { get; set; }

        // Relación con Ventas
        public ICollection<Venta> Ventas { get; set; }
    }
}
