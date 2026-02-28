using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RefWeb.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }

        public string UsuarioId { get; set; }
        [ForeignKey("UsuarioId")]
        public IdentityUser Usuario { get; set; }

        [StringLength(50)]
        public string Accion { get; set; } // 'INSERT', 'UPDATE', 'DELETE', 'LOGIN'

        [StringLength(100)]
        public string Entidad { get; set; } // 'Ventas', 'Productos', etc.

        public int? EntidadId { get; set; } // ID del registro afectado

        [Column(TypeName = "longtext")]
        public string DatosAnteriores { get; set; } // JSON con estado anterior

        [Column(TypeName = "longtext")]
        public string DatosNuevos { get; set; } // JSON con estado nuevo

        public DateTime Fecha { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string IPDireccion { get; set; }

        [StringLength(500)]
        public string Navegador { get; set; }
    }
}
