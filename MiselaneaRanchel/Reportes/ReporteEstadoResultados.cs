using System;
using System.Collections.Generic;
using System.Linq;
using MiselaneaRanchel.Data;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using ClosedXML.Excel;
using MiselaneaRanchel.Models;
using iText.Layout.Properties;
using iText.Kernel.Font;

namespace MiselaneaRanchel.Reportes
{
    public class ReporteEstadoResultados
    {
        public List<ReporteItemFinanciero> ObtenerDatos(ApplicationDbContext context, DateTime inicio, DateTime fin, out decimal utilidadNeta)
        {
            decimal ventas = context.Ventas.Where(v => v.FechaVenta >= inicio && v.FechaVenta <= fin && v.Estado == "COMPLETADO").Sum(v => (decimal?)v.TotalVenta) ?? 0m;
            decimal costo = context.DetalleVentas.Include(d => d.Venta).Where(d => d.Venta.FechaVenta >= inicio && d.Venta.FechaVenta <= fin && d.Venta.Estado == "COMPLETADO").Sum(d => (decimal?)(d.PrecioCostoHistorico * d.Cantidad)) ?? 0m;
            decimal gastos = context.Compras.Where(c => c.FechaCompra >= inicio && c.FechaCompra <= fin && c.Estado == "COMPLETADO").Sum(c => (decimal?)c.TotalCompra) ?? 0m;

            decimal utilidadBruta = ventas - costo;
            utilidadNeta = utilidadBruta - gastos;

            return new List<ReporteItemFinanciero>
            {
                new ReporteItemFinanciero { Concepto = "Ingresos por Ventas", Monto = ventas.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "(-) Costo de lo Vendido", Monto = $"- {costo:C2}" },
                new ReporteItemFinanciero { Concepto = "-----------------------------", Monto = "" },
                new ReporteItemFinanciero { Concepto = "= UTILIDAD BRUTA", Monto = utilidadBruta.ToString("C2") },
                new ReporteItemFinanciero { Concepto = "(-) Gastos y Compras", Monto = $"- {gastos:C2}" },
                new ReporteItemFinanciero { Concepto = "-----------------------------", Monto = "" },
                new ReporteItemFinanciero { Concepto = "= UTILIDAD NETA DEL EJERCICIO", Monto = utilidadNeta.ToString("C2") }
            };
        }

        public void GenerarPDF(string ruta, List<ReporteItemFinanciero> datos, DateTime inicio, DateTime fin)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            Color colorPrincipal = new DeviceRgb(44, 62, 80);
            Color colorFilaPar = new DeviceRgb(236, 240, 241);
            Color colorBlanco = ColorConstants.WHITE;

            document.Add(new Paragraph("MINISUPER MAYORGA").SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(colorPrincipal));
            document.Add(new Paragraph("REPORTE: Estado de Resultados").SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));

            document.Add(new Paragraph($"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}\nPeriodo evaluado: {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}")
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

        public void GenerarExcel(string ruta, List<ReporteItemFinanciero> datos)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Estado de Resultados");

            var rangoTitulo = worksheet.Range("A1:C1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA";
            rangoTitulo.Style.Font.FontSize = 22;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Row(1).Height = 35;

            var rangoSub = worksheet.Range("A2:C2");
            rangoSub.Merge().Value = "REPORTE: Estado de Resultados";
            rangoSub.Style.Font.FontSize = 14;
            rangoSub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range("A3:C3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int filaInicio = 5;
            worksheet.Cell(filaInicio, 1).Value = "Item";
            worksheet.Cell(filaInicio, 2).Value = "Concepto";
            worksheet.Cell(filaInicio, 3).Value = "Monto";

            var cabeceraEstilo = worksheet.Range(filaInicio, 1, filaInicio, 3).Style;
            cabeceraEstilo.Font.FontColor = XLColor.White;
            cabeceraEstilo.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            cabeceraEstilo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

            var rangoTabla = worksheet.Range(filaInicio, 1, filaActual - 1, 3);
            rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            worksheet.Range(filaInicio + 1, 1, filaActual - 1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(ruta);
        }
    }
}