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
    

    public class ReporteStock
    {
        public List<ItemStock> ObtenerDatos(ApplicationDbContext context)
        {
            return context.Productos
                .Include(p => p.Categoria)
                .Where(p => p.Activo == true)
                .Select(p => new ItemStock
                {
                    Codigo = p.CodigoBarras,
                    Descripcion = p.Descripcion,
                    Categoria = p.Categoria.Nombre,
                    StockActual = p.StockActual,
                    CostoUnitario = p.PrecioCosto
                })
                .OrderBy(p => p.Categoria)
                .ThenBy(p => p.Descripcion)
                .ToList();
        }

        public void GenerarPDF(string ruta, List<ItemStock> datos, DateTime fechaEmision)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            Color colorPrincipal = new DeviceRgb(44, 62, 80);
            Color colorFilaPar = new DeviceRgb(236, 240, 241);
            Color colorBlanco = ColorConstants.WHITE;

            document.Add(new Paragraph("MINISUPER MAYORGA").SetTextAlignment(TextAlignment.CENTER).SetFontSize(24).SetFontColor(colorPrincipal));
            document.Add(new Paragraph("REPORTE: Stock Actual en Bodega").SetFontSize(14).SetTextAlignment(TextAlignment.CENTER));
            document.Add(new Paragraph($"Fecha de Emisión: {fechaEmision:dd/MM/yyyy HH:mm}").SetTextAlignment(TextAlignment.RIGHT).SetFontSize(10).SetMarginBottom(15));

            Table tabla = new Table(new float[] { 1.5f, 4f, 2f, 1.5f, 2f, 2f }).UseAllAvailableWidth();

            string[] cabeceras = { "Código", "Producto", "Categoría", "Stock", "Costo Unit.", "Valor Total" };
            foreach (var cabecera in cabeceras)
            {
                tabla.AddHeaderCell(new Cell().Add(new Paragraph(cabecera))
                    .SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.CENTER).SetPadding(5));
            }

            int itemContador = 1;
            decimal granTotalValor = 0;

            foreach (var item in datos)
            {
                bool esPar = itemContador % 2 == 0;
                Color fondoCelda = esPar ? colorFilaPar : colorBlanco;

                tabla.AddCell(new Cell().Add(new Paragraph(item.Codigo)).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Descripcion)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Categoria)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.StockActual.ToString("N2"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.CostoUnitario.ToString("C$ #,##0.00"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));
                tabla.AddCell(new Cell().Add(new Paragraph(item.ValorTotal.ToString("C$ #,##0.00"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));

                granTotalValor += item.ValorTotal;
                itemContador++;
            }

            // Fila de total
            tabla.AddCell(new Cell(1, 5).Add(new Paragraph("VALOR TOTAL DEL INVENTARIO:")).SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));
            tabla.AddCell(new Cell().Add(new Paragraph(granTotalValor.ToString("C$ #,##0.00"))).SetBackgroundColor(colorPrincipal).SetFontColor(colorBlanco).SetTextAlignment(TextAlignment.RIGHT).SetPadding(5));

            document.Add(tabla);
            document.Close();
        }

        public void GenerarExcel(string ruta, List<ItemStock> datos)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Stock Actual");

            var rangoTitulo = worksheet.Range("A1:F1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA - REPORTE DE STOCK ACTUAL";
            rangoTitulo.Style.Font.FontSize = 18;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range("A3:F3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int filaInicio = 5;
            string[] cabeceras = { "Código", "Producto", "Categoría", "Stock", "Costo Unit.", "Valor Total" };
            for (int i = 0; i < cabeceras.Length; i++)
                worksheet.Cell(filaInicio, i + 1).Value = cabeceras[i];

            worksheet.Range(filaInicio, 1, filaInicio, 6).Style.Font.FontColor = XLColor.White;
            worksheet.Range(filaInicio, 1, filaInicio, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");

            int filaActual = filaInicio + 1;
            foreach (var item in datos)
            {
                worksheet.Cell(filaActual, 1).Value = item.Codigo;
                worksheet.Cell(filaActual, 2).Value = item.Descripcion;
                worksheet.Cell(filaActual, 3).Value = item.Categoria;
                worksheet.Cell(filaActual, 4).Value = item.StockActual;
                worksheet.Cell(filaActual, 5).Value = item.CostoUnitario;
                worksheet.Cell(filaActual, 6).Value = item.ValorTotal;
                filaActual++;
            }

            worksheet.Range(filaInicio + 1, 5, filaActual - 1, 6).Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(ruta);
        }
    }
}