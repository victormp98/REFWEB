using System.ComponentModel.DataAnnotations;

namespace RefWeb.Models
{
    public class ConfiguracionNegocio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Direccion { get; set; }

        [StringLength(50)]
        public string Telefono { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(20)]
        public string RFC { get; set; }

        [StringLength(500)]
        public string LogoUrl { get; set; }

        [StringLength(500)]
        public string LeyendaPie { get; set; }

        [StringLength(100)]
        public string ImpresoraPredeterminada { get; set; }

        public bool Activo { get; set; } = true;
    }
}
