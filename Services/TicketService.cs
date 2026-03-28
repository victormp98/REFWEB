using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;
using Microsoft.EntityFrameworkCore;
using RefWeb.Data;
using RefWeb.Models;
using System.Text;

namespace RefWeb.Services
{
    public interface ITicketService
    {
        Task<byte[]> GenerarTicketVentaAsync(Venta venta);
    }

    public class TicketService : ITicketService
    {
        private readonly IConverter _converter;
        private readonly ApplicationDbContext _context;

        public TicketService(IConverter converter, ApplicationDbContext context)
        {
            _converter = converter;
            _context = context;
        }

        public async Task<byte[]> GenerarTicketVentaAsync(Venta venta)
        {
            // Leer configuración dinámica del negocio
            var config = await _context.ConfiguracionNegocio.FirstOrDefaultAsync();
            string nombreNegocio = config?.Nombre ?? "Refaccionaria Web";
            string rfc = config?.RFC ?? "";
            string telefono = config?.Telefono ?? "";
            string leyenda = config?.LeyendaPie ?? "¡Gracias por su compra!";

            var sb = new StringBuilder();
            sb.Append($@"
<html>
<head>
    <style>
        body {{ font-family: 'Courier New', Courier, monospace; width: 80mm; margin: 0; padding: 2mm; font-size: 14pt; box-sizing: border-box; }}
        .header {{ text-align: center; margin-bottom: 10px; }}
        .header h3 {{ margin: 0; font-size: 14px; }}
        .header p {{ margin: 2px 0; font-size: 11px; }}
        .footer {{ text-align: center; margin-top: 10px; font-size: 10px; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th {{ border-bottom: 1px solid #000; text-align: left; font-size: 11px; }}
        td {{ font-size: 11px; padding: 2px 0; }}
        .total-row {{ border-top: 2px solid #000; font-weight: bold; }}
        .text-right {{ text-align: right; }}
    </style>
</head>
<body>
    <div class='header'>
        <h3>{nombreNegocio}</h3>
        {(string.IsNullOrEmpty(rfc) ? "" : $"<p>RFC: {rfc}</p>")}
        {(string.IsNullOrEmpty(telefono) ? "" : $"<p>Tel: {telefono}</p>")}
        <p>Folio: {venta.Folio}</p>
        <p>Fecha: {venta.Fecha:dd/MM/yyyy HH:mm}</p>
    </div>
    <hr>
    <table>
        <thead>
            <tr>
                <th>Cant.</th>
                <th>Prod.</th>
                <th class='text-right'>Total</th>
            </tr>
        </thead>
        <tbody>");

            foreach (var d in venta.VentasDetalle)
            {
                sb.Append($@"<tr>
<td>{d.Cantidad}</td>
<td>{d.Producto?.Nombre}</td>
<td class='text-right'>${d.Subtotal:N2}</td>
</tr>");
            }

            sb.Append($@"
        </tbody>
    </table>
    <hr>
    <div class='text-right'>
        <p><strong>Total: ${venta.Total:N2}</strong></p>
        <p>Método: {venta.MetodoPago}</p>
    </div>
    <div class='footer'>
        <p>{leyenda}</p>
    </div>
</body>
</html>");

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings =
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 0, Bottom = 0, Left = 0, Right = 0, Unit = Unit.Millimeters }
                },
                Objects =
                {
                    new ObjectSettings()
                    {
                        PagesCount = true,
                        HtmlContent = sb.ToString(),
                        WebSettings = { DefaultEncoding = "utf-8" },
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
