using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class Merma
    {
        [Key]
        public int Id { get; set; }

        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public Producto Producto { get; set; }

        [Required]
        [Range(1, 100000)]
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string TipoMerma { get; set; } // 'Caducidad', 'Rotura', 'Robo', 'Devolución'

        [StringLength(500)]
        public string Motivo { get; set; }

        public string ResponsableId { get; set; } // IdentityUser que reporta
        [ForeignKey("ResponsableId")]
        public IdentityUser Responsable { get; set; }

        public string AutorizadoPorId { get; set; } // IdentityUser que autoriza
        [ForeignKey("AutorizadoPorId")]
        public IdentityUser AutorizadoPor { get; set; }

        [StringLength(500)]
        public string ComprobanteUrl { get; set; } // Foto del producto dañado

        [StringLength(500)]
        public string Notas { get; set; }
    }
}
