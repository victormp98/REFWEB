using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class Direccion
    {
        [Key]
        public int Id { get; set; }

        public int ClienteId { get; set; }
        [ForeignKey("ClienteId")]
        public Cliente Cliente { get; set; }

        [StringLength(20)]
        public string TipoDireccion { get; set; } // 'Casa', 'Oficina', 'Sucursal'

        [Required]
        [StringLength(200)]
        public string Calle { get; set; }

        [StringLength(100)]
        public string Colonia { get; set; }

        [Required]
        [StringLength(100)]
        public string Ciudad { get; set; }

        [Required]
        [StringLength(100)]
        public string Estado { get; set; }

        [Required]
        [StringLength(10)]
        public string CodigoPostal { get; set; }

        [StringLength(500)]
        public string Referencias { get; set; }

        public bool EsPrincipal { get; set; } = false;

        // Soft Delete
        public bool Activo { get; set; } = true;
        public DateTime? FechaEliminacion { get; set; }
    }
}
