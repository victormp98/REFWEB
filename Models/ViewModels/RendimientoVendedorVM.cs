namespace RefWeb.Models.ViewModels
{
    public class RendimientoVendedorVM
    {
        public string Vendedor { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Tickets { get; set; } = 0;
        public decimal TotalVendido { get; set; } = 0;
    }
}
