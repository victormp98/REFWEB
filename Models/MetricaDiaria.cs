using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class MetricaDiaria
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVentas { get; set; }

        [Range(0, int.MaxValue)]
        public int NumeroVentas { get; set; }
        [Range(0, int.MaxValue)]
        public int ProductosVendidos { get; set; }
        [Range(0, int.MaxValue)]
        public int ClientesNuevos { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IngresosEfectivo { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IngresosTarjeta { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal IngresosOnline { get; set; }

        [Range(0, int.MaxValue)]
        public int ProductosBajoStock { get; set; }

        public DateTime FechaCalculo { get; set; } = DateTime.Now;
    }
}
