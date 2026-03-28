using RefWeb.Models;

namespace RefWeb.Services
{
    public interface IVentasService
    {
        Task<(bool Success, string Message, Venta? Venta)> ProcesarVentaAsync(Venta venta, string userId);
        Task<List<Venta>> ObtenerVentasPorCorteAsync(int corteCajaId);
        bool ValidarMetodoPago(string metodoPago, string tipoVenta);
        Task<(bool Success, string Message, Merma? Merma)> RegistrarMermaAsync(Merma merma, string userId);
        Task<(bool Success, string Message)> CancelarPedidoAsync(int pedidoId, string userId, string motivo = "Cancelado por el Administrador");
    }
}
