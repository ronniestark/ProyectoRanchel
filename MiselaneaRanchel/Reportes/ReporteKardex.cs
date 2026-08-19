using System;
using System.Collections.Generic;
using System.Linq;
using MiselaneaRanchel.Data;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.Layout.Properties;
using ClosedXML.Excel;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Reportes
{


    public class ReporteKardex
    {
        public List<ItemKardex> ObtenerDatos(ApplicationDbContext context, DateTime inicio, DateTime fin)
        {
            return context.MovimientosInventario
                .Include(m => m.Producto)
                .Where(m => m.FechaMovimiento >= inicio && m.FechaMovimiento <= fin)
                .Select(m => new ItemKardex
                {
                    Fecha = m.FechaMovimiento,
                    Producto = m.Producto.Descripcion,
                    Tipo = m.TipoMovimiento,
                    Cantidad = m.Cantidad,
                    Motivo = m.Motivo
                })
                .OrderBy(m => m.Fecha)
                .ToList();
        }

        public void GenerarPDF(string ruta, List<ItemKardex> datos, DateTime inicio, DateTime fin)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            Color colorPrincipal = new DeviceRgb(44, 62, 80);
            Color colorFilaPar = new DeviceRgb(236, 240, 241);
            Color colorBlanco = ColorConstants.WHITE;

            document.Add(new Paragraph("MINISUPER MAYORGA").SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(colorPrincipal));
            document.Add(new Paragraph("REPORTE: Kardex (Movimientos de Inventario)").SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Periodo: {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}").SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10).SetMarginBottom(15));

            Table tabla = new Table(new float[] { 2f, 4f, 1.5f, 1.5f, 3f }).UseAllAvailableWidth();

            string[] cabeceras = { "Fecha", "Producto", "Tipo", "Cantidad", "Motivo" };
            foreach (var cabecera in cabeceras)
            {
                tabla.AddHeaderCell(new Cell().Add(new Paragraph(cabecera))
                    .SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
            }

            int itemContador = 1;
            foreach (var item in datos)
            {
                bool esPar = itemContador % 2 == 0;
                Color fondoCelda = esPar ? colorFilaPar : colorBlanco;

                // Color para entradas y salidas
                Color colorTipo = item.Tipo == "ENTRADA" ? new DeviceRgb(39, 174, 96) : new DeviceRgb(231, 76, 60);

                tabla.AddCell(new Cell().Add(new Paragraph(item.Fecha.ToString("dd/MM/yy HH:mm"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Producto)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Tipo)).SetBackgroundColor(fondoCelda).SetFontColor(colorTipo).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Cantidad.ToString("N2"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Motivo)).SetBackgroundColor(fondoCelda));

                itemContador++;
            }

            document.Add(tabla);
            document.Close();
        }

        public void GenerarExcel(string ruta, List<ItemKardex> datos)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Kardex");

            var rangoTitulo = worksheet.Range("A1:E1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA - KARDEX DE INVENTARIO";
            rangoTitulo.Style.Font.FontSize = 18;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range("A3:E3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int filaInicio = 5;
            string[] cabeceras = { "Fecha", "Producto", "Tipo", "Cantidad", "Motivo" };
            for (int i = 0; i < cabeceras.Length; i++)
                worksheet.Cell(filaInicio, i + 1).Value = cabeceras[i];

            worksheet.Range(filaInicio, 1, filaInicio, 5).Style.Font.FontColor = XLColor.White;
            worksheet.Range(filaInicio, 1, filaInicio, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");

            int filaActual = filaInicio + 1;
            foreach (var item in datos)
            {
                worksheet.Cell(filaActual, 1).Value = item.Fecha.ToString("dd/MM/yyyy HH:mm");
                worksheet.Cell(filaActual, 2).Value = item.Producto;
                worksheet.Cell(filaActual, 3).Value = item.Tipo;
                worksheet.Cell(filaActual, 4).Value = item.Cantidad;
                worksheet.Cell(filaActual, 5).Value = item.Motivo;
                filaActual++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(ruta);
        }
    }
}