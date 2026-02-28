using ClosedXML.Excel;
using WkHtmlToPdfDotNet;
using WkHtmlToPdfDotNet.Contracts;
using RefWeb.Data;
using RefWeb.Models;
using System.Text;

namespace RefWeb.Services
{
    public class ExportService
    {
        private readonly IConverter _converter;
        private readonly ApplicationDbContext _context;

        public ExportService(IConverter converter, ApplicationDbContext context)
        {
            _converter = converter;
            _context = context;
        }

        // ── EXCEL ─────────────────────────────────────────────────────────
        public byte[] ExportarVentasExcel(List<Venta> ventas)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ventas");

            // Cabeceras
            ws.Cell(1, 1).Value = "Folio";
            ws.Cell(1, 2).Value = "Fecha";
            ws.Cell(1, 3).Value = "Tipo";
            ws.Cell(1, 4).Value = "Método Pago";
            ws.Cell(1, 5).Value = "Subtotal";
            ws.Cell(1, 6).Value = "Total";
            ws.Cell(1, 7).Value = "Estado";

            var headerRow = ws.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            headerRow.Style.Font.FontColor = XLColor.FromHtml("#FFFFFF");

            // Datos
            int row = 2;
            foreach (var v in ventas)
            {
                ws.Cell(row, 1).Value = v.Folio;
                ws.Cell(row, 2).Value = v.Fecha.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 3).Value = v.TipoVenta ?? "-";
                ws.Cell(row, 4).Value = v.MetodoPago ?? "-";
                ws.Cell(row, 5).Value = v.Subtotal;
                ws.Cell(row, 6).Value = v.Total;
                ws.Cell(row, 7).Value = v.Estado ?? "-";

                // Fila alternada
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");

                row++;
            }

            // Totales
            ws.Cell(row, 4).Value = "TOTAL:";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 6).Value = ventas.Sum(v => v.Total);
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";

            // Formatear columnas de dinero
            ws.Columns(5, 6).Style.NumberFormat.Format = "#,##0.00";

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ── PDF ───────────────────────────────────────────────────────────
        public byte[] ExportarVentasPDF(List<Venta> ventas, string nombreNegocio = "RefWeb")
        {
            var sb = new StringBuilder();
            sb.Append($@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  body {{ font-family: Arial, sans-serif; font-size: 12px; margin: 20px; }}
  h1 {{ text-align: center; color: #2C3E50; font-size: 18px; margin-bottom: 4px; }}
  p.subtitle {{ text-align: center; color: #7f8c8d; margin: 0 0 16px; }}
  table {{ width: 100%; border-collapse: collapse; }}
  th {{ background: #2C3E50; color: white; padding: 8px; text-align: left; }}
  td {{ padding: 6px 8px; border-bottom: 1px solid #ecf0f1; }}
  tr:nth-child(even) td {{ background: #f9f9f9; }}
  .total-row td {{ font-weight: bold; background: #ecf0f1; }}
  .text-right {{ text-align: right; }}
  .footer {{ text-align: center; margin-top: 24px; font-size: 10px; color: #95a5a6; }}
</style>
</head>
<body>
<h1>Reporte de Ventas — {nombreNegocio}</h1>
<p class='subtitle'>Generado el {DateTime.Now:dd/MM/yyyy HH:mm}</p>
<table>
<thead>
  <tr>
    <th>Folio</th><th>Fecha</th><th>Tipo</th><th>Método</th>
    <th class='text-right'>Subtotal</th>
    <th class='text-right'>Total</th><th>Estado</th>
  </tr>
</thead>
<tbody>");

            foreach (var v in ventas)
            {
                sb.Append($@"<tr>
  <td>{v.Folio}</td>
  <td>{v.Fecha:dd/MM/yyyy HH:mm}</td>
  <td>{v.TipoVenta ?? "-"}</td>
  <td>{v.MetodoPago ?? "-"}</td>
  <td class='text-right'>${v.Subtotal:N2}</td>
  <td class='text-right'>${v.Total:N2}</td>
  <td>{v.Estado ?? "-"}</td>
</tr>");
            }

            decimal grandTotal = ventas.Sum(v => v.Total);
            sb.Append($@"</tbody>
<tfoot>
  <tr class='total-row'>
    <td colspan='5' class='text-right'>TOTAL GENERAL:</td>
    <td class='text-right'>${grandTotal:N2}</td>
    <td></td>
  </tr>
</tfoot>
</table>
<div class='footer'>
  <p>Registros: {ventas.Count} venta(s) &mdash; {nombreNegocio}</p>
</div>
</body></html>");

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Landscape,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        PagesCount = true,
                        HtmlContent = sb.ToString(),
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
