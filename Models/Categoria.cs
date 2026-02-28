using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RefWeb.Models
{
    public class Categoria
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        // Soft Delete
        public bool Activo { get; set; } = true;

        // Auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        public DateTime? FechaEliminacion { get; set; }

        // Relaciones
        public ICollection<Producto> Productos { get; set; }
    }
}
