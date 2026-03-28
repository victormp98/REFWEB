using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class Merma
    {
        public Merma()
        {
            TipoMerma = string.Empty;
            Motivo = string.Empty;
            ResponsableId = string.Empty;
            ComprobanteUrl = string.Empty;
            Notas = string.Empty;
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un producto.")]
        public int ProductoId { get; set; }
        [ForeignKey("ProductoId")]
        public Producto? Producto { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, 100000, ErrorMessage = "La cantidad debe ser mayor a 0.")]
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Debes indicar el tipo de merma.")]
        [StringLength(50)]
        public string TipoMerma { get; set; } // 'Caducidad', 'Rotura', 'Robo', 'Devolución'

        [Required(ErrorMessage = "Debes explicar el motivo de la baja.")]
        [StringLength(500)]
        public string Motivo { get; set; }

        public string ResponsableId { get; set; } // IdentityUser que reporta
        [ForeignKey("ResponsableId")]
        public IdentityUser? Responsable { get; set; }

        public string? AutorizadoPorId { get; set; } // IdentityUser que autoriza
        [ForeignKey("AutorizadoPorId")]
        public IdentityUser? AutorizadoPor { get; set; }

        [StringLength(500)]
        public string? ComprobanteUrl { get; set; } // Foto del producto dañado

        [StringLength(500)]
        public string? Notas { get; set; }
    }
}

