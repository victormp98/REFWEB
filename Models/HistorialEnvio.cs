using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class HistorialEnvio
    {
        [Key]
        public int Id { get; set; }

        public int EnvioId { get; set; }
        [ForeignKey("EnvioId")]
        public Envio Envio { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Estado { get; set; }

        [StringLength(200)]
        public string Ubicacion { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }
    }
}
