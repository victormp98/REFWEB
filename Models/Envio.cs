using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RefWeb.Models
{
    public class Envio
    {
        [Key]
        public int Id { get; set; }

        public int PedidoId { get; set; }
        [ForeignKey("PedidoId")]
        public Pedido Pedido { get; set; }

        [StringLength(100)]
        public string NumeroGuia { get; set; }

        [StringLength(50)]
        public string Paqueteria { get; set; } // DHL, FedEx, Estafeta, etc.

        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEstimadaEntrega { get; set; }
        public DateTime? FechaEntrega { get; set; }

        [StringLength(30)]
        public string EstadoEnvio { get; set; } = "Preparando"; // Preparando, Enviado, En tránsito, Entregado

        [StringLength(500)]
        public string UrlRastreo { get; set; }

        [StringLength(500)]
        public string Notas { get; set; }

        // Relación con historial
        public ICollection<HistorialEnvio> HistorialEnvios { get; set; }
    }
}
