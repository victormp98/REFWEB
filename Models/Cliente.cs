using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        // Relación con IdentityUser (AspNetUsers)
        public string UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public IdentityUser Usuario { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string Apellidos { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Telefono { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Soft Delete
        public bool Activo { get; set; } = true;
        public DateTime? FechaEliminacion { get; set; }

        // Relaciones
        public ICollection<Direccion> Direcciones { get; set; }
        public ICollection<Pedido> Pedidos { get; set; }
        
        [InverseProperty("Cliente")]
        public ICollection<Venta> Ventas { get; set; }
    }
}
