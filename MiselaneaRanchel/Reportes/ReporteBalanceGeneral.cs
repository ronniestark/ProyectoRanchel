
using MiselaneaRanchel.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using ClosedXML.Excel;
using MiselaneaRanchel.Models;
using iText.Layout.Properties;

namespace MiselaneaRanchel.Reportes
{
    public class ReporteBalanceGeneral
    {
        public List<ReporteItemFinanciero> ObtenerDatos(ApplicationDbContext context, DateTime fin, out decimal totalActivos)
        {
            decimal valorInventario = context.Productos.Where(p => p.Activo == true).Sum(p => (decimal?)(p.StockActual * p.PrecioCosto)) ?? 0m;
            decimal totalVentasHist = context.Ventas.Where(v => v.FechaVenta <= fin && v.Estado == "COMPLETADO").Sum(v => (decimal?)v.TotalVenta) ?? 0m;
            decimal totalComprasHist = context.Compras.Where(c => c.FechaCompra <= fin && c.Estado == "COMPLETADO").Sum(c => (decimal?)c.TotalCompra) ?? 0m;

            decimal efectivoCaja = totalVentasHist - totalComprasHist;
            totalActivos = valorInventario + efectivoCaja;

            return new List<ReporteItemFinanciero>
            {
                new ReporteItemFinanciero { Concepto = "ACTIVOS", Monto = "" },
                new ReporteItemFinanciero { Concepto = "  ▶ Efectivo o Equivalentes (Caja)", Monto = efectivoCaja.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "  ▶ Inventario de Mercancía", Monto = valorInventario.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "-----------------------------", Monto = "" },
                new ReporteItemFinanciero { Concepto = "= TOTAL ACTIVOS", Monto = totalActivos.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "", Monto = "" },
                new ReporteItemFinanciero { Concepto = "PASIVOS Y CAPITAL", Monto = "" },
                new ReporteItemFinanciero { Concepto = "  ▶ Pasivos (Deudas)", Monto = "$ 0.00" },
                new ReporteItemFinanciero { Concepto = "  ▶ Capital Contable", Monto = totalActivos.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "-----------------------------", Monto = "" },
                new ReporteItemFinanciero { Concepto = "= TOTAL PASIVO Y CAPITAL", Monto = totalActivos.ToString("C2") }
            };
        }

        public void GenerarPDF(string ruta, List<ReporteItemFinanciero> datos, DateTime fin)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            Color colorPrincipal = new DeviceRgb(44, 62, 80);
            Color colorFilaPar = new DeviceRgb(236, 240, 241);
            Color colorBlanco = ColorConstants.WHITE;

            document.Add(new Paragraph("MINISUPER MAYORGA").SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(colorPrincipal));
            document.Add(new Paragraph("REPORTE: Balance General").SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}\nAl día de corte: {fin:dd/MM/yyyy}")
                .SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10).SetMarginBottom(15));

            Table tabla = new Table(new float[] { 1f, 4f, 2f }).UseAllAvailableWidth();

            string[] cabeceras = { "Item", "Cuenta Contable", "Balance" };
            foreach (var cabecera in cabeceras)
            {
                tabla.AddHeaderCell(new Cell().Add(new Paragraph(cabecera))
                    .SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
            }

            int itemContador = 1;
            foreach (var item in datos)
            {
                bool esTotalOTitulo = item.Concepto.Contains("=") || item.Concepto.Contains("---") || string.IsNullOrWhiteSpace(item.Monto);
                bool esPar = itemContador % 2 == 0;
                Color fondoCelda = esTotalOTitulo ? colorBlanco : (esPar ? colorFilaPar : colorBlanco);

                string numItem = esTotalOTitulo ? "" : itemContador.ToString();

                tabla.AddCell(new Cell().Add(new Paragraph(numItem)).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Concepto)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Monto)).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));

                if (!esTotalOTitulo && !string.IsNullOrWhiteSpace(item.Concepto)) itemContador++;
            }
            document.Add(tabla);
            document.Close();
        }

        public void GenerarExcel(string ruta, List<ReporteItemFinanciero> datos)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Balance General");

            var rangoTitulo = worksheet.Range("A1:C1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA";
            rangoTitulo.Style.Font.FontSize = 22;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Row(1).Height = 35;

            var rangoSub = worksheet.Range("A2:C2");
            rangoSub.Merge().Value = "REPORTE: Balance General";
            rangoSub.Style.Font.FontSize = 14;
            rangoSub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range("A3:C3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int filaInicio = 5;
            worksheet.Cell(filaInicio, 1).Value = "Item";
            worksheet.Cell(filaInicio, 2).Value = "Cuenta Contable";
            worksheet.Cell(filaInicio, 3).Value = "Balance";

            var cabeceraEstilo = worksheet.Range(filaInicio, 1, filaInicio, 3).Style;
            cabeceraEstilo.Font.FontColor = XLColor.White;
            cabeceraEstilo.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            cabeceraEstilo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int filaActual = filaInicio + 1;
            int itemContador = 1;

            foreach (var item in datos)
            {
                bool esTotalOTitulo = item.Concepto.Contains("=") || item.Concepto.Contains("---") || string.IsNullOrWhiteSpace(item.Monto);

                if (!esTotalOTitulo && !string.IsNullOrWhiteSpace(item.Concepto))
                {
                    worksheet.Cell(filaActual, 1).Value = itemContador;
                    itemContador++;
                }

                worksheet.Cell(filaActual, 2).Value = item.Concepto;
                worksheet.Cell(filaActual, 3).Value = item.Monto;

                if (esTotalOTitulo && item.Concepto.Contains("="))
                {
                    worksheet.Range(filaActual, 1, filaActual, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECF0F1");
                }
                filaActual++;
            }

            var rangoTabla = worksheet.Range(filaInicio, 1, filaActual - 1, 3);
            rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(filaInicio + 1, 1, filaActual - 1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(ruta);
        }
    }
}