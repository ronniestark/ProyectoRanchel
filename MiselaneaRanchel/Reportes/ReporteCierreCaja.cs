using ClosedXML.Excel;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;
using iText.Layout.Properties;

namespace MiselaneaRanchel.Reportes
{
    public class ReporteCierreCaja
    {
        public List<ReporteItemFinanciero> ObtenerDatos(ApplicationDbContext context, DateTime fecha)
        {
            // Solo toma los registros del DÍA exacto seleccionado
            DateTime inicioDia = fecha.Date;
            DateTime finDia = fecha.Date.AddDays(1).AddTicks(-1);

            // Suma ventas
            decimal totalVentas = context.Ventas
                .Where(v => v.FechaVenta >= inicioDia && v.FechaVenta <= finDia && v.Estado == "COMPLETADO")
                .Sum(v => (decimal?)v.TotalVenta) ?? 0m;

            int cantidadTickets = context.Ventas
                .Count(v => v.FechaVenta >= inicioDia && v.FechaVenta <= finDia && v.Estado == "COMPLETADO");

            // Suma gastos/compras de ese día
            decimal totalCompras = context.Compras
                .Where(c => c.FechaCompra >= inicioDia && c.FechaCompra <= finDia && c.Estado == "COMPLETADO")
                .Sum(c => (decimal?)c.TotalCompra) ?? 0m;

            decimal totalEnCaja = totalVentas - totalCompras;

            return new List<ReporteItemFinanciero>
            {
                new ReporteItemFinanciero { Concepto = "Total Ingresos por Ventas POS", Monto = totalVentas.ToString("C$ #,##0.00") },
                new ReporteItemFinanciero { Concepto = $"Tickets Emitidos: {cantidadTickets}", Monto = "" },
                new ReporteItemFinanciero { Concepto = "(-) Total Gastos / Compras pagadas", Monto = $"- {totalCompras:C$ #,##0.00}" },
                new ReporteItemFinanciero { Concepto = "-----------------------------", Monto = "" },
                new ReporteItemFinanciero { Concepto = "= TOTAL ESTIMADO EN CAJA", Monto = totalEnCaja.ToString("C$ #,##0.00") }
            };
        }

        public void GenerarPDF(string ruta, List<ReporteItemFinanciero> datos, DateTime fecha)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            Color colorPrincipal = new DeviceRgb(44, 62, 80);
            Color colorFilaPar = new DeviceRgb(236, 240, 241);
            Color colorBlanco = ColorConstants.WHITE;

            document.Add(new Paragraph("MINISUPER MAYORGA").SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(colorPrincipal));
            document.Add(new Paragraph("REPORTE: Cierre de Caja Z").SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Fecha de Cierre: {fecha:dd/MM/yyyy}\nGenerado el: {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10).SetMarginBottom(15));

            Table tabla = new Table(new float[] { 1f, 4f, 2f }).UseAllAvailableWidth();

            string[] cabeceras = { "Item", "Concepto", "Monto" };
            foreach (var cabecera in cabeceras)
            {
                tabla.AddHeaderCell(new Cell().Add(new Paragraph(cabecera))
                    .SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
            }

            int itemContador = 1;
            foreach (var item in datos)
            {
                bool esTotalOSeparador = item.Concepto.Contains("=") || item.Concepto.Contains("---") || string.IsNullOrWhiteSpace(item.Monto);
                bool esPar = itemContador % 2 == 0;
                Color fondoCelda = esTotalOSeparador ? colorBlanco : (esPar ? colorFilaPar : colorBlanco);

                string numItem = esTotalOSeparador ? "" : itemContador.ToString();

                tabla.AddCell(new Cell().Add(new Paragraph(numItem)).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Concepto)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Monto)).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));

                if (!esTotalOSeparador) itemContador++;
            }
            document.Add(tabla);
            document.Close();
        }

        public void GenerarExcel(string ruta, List<ReporteItemFinanciero> datos, DateTime fecha)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Cierre de Caja");

            var rangoTitulo = worksheet.Range("A1:C1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA - CIERRE DE CAJA";
            rangoTitulo.Style.Font.FontSize = 18;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Día de Cierre: {fecha:dd/MM/yyyy}";
            worksheet.Range("A3:C3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int filaInicio = 5;
            worksheet.Cell(filaInicio, 1).Value = "Item";
            worksheet.Cell(filaInicio, 2).Value = "Concepto";
            worksheet.Cell(filaInicio, 3).Value = "Monto";

            var cabeceraEstilo = worksheet.Range(filaInicio, 1, filaInicio, 3).Style;
            cabeceraEstilo.Font.FontColor = XLColor.White;
            cabeceraEstilo.Fill.BackgroundColor = XLColor.FromHtml("#34495E");

            int filaActual = filaInicio + 1;
            int itemContador = 1;

            foreach (var item in datos)
            {
                bool esTotalOSeparador = item.Concepto.Contains("=") || item.Concepto.Contains("---") || string.IsNullOrWhiteSpace(item.Monto);

                if (!esTotalOSeparador)
                {
                    worksheet.Cell(filaActual, 1).Value = itemContador;
                    itemContador++;
                }

                worksheet.Cell(filaActual, 2).Value = item.Concepto;
                worksheet.Cell(filaActual, 3).Value = item.Monto;

                if (esTotalOSeparador && item.Concepto.Contains("="))
                {
                    worksheet.Range(filaActual, 1, filaActual, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECF0F1");
                }
                filaActual++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(ruta);
        }
    }
}