using System;
using System.Collections.Generic;
using System.Linq;
using MiselaneaRanchel.Data;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using ClosedXML.Excel;
using MiselaneaRanchel.Models;
using iText.Kernel.Font;

namespace MiselaneaRanchel.Reportes
{
    
    public class ReporteAsientoContable
    {
        public List<AsientoItem> ObtenerDatos(ApplicationDbContext context, DateTime inicio, DateTime fin)
        {
            var ventas = context.Ventas
                .Where(v => v.FechaVenta >= inicio && v.FechaVenta <= fin && v.Estado == "COMPLETADO")
                .Select(v => new AsientoItem { Fecha = v.FechaVenta, Concepto = "Venta Mostrador", Referencia = v.NumeroTicket, Ingreso_Debe = v.TotalVenta, Egreso_Haber = 0 })
                .ToList();

            var compras = context.Compras
                .Where(c => c.FechaCompra >= inicio && c.FechaCompra <= fin && c.Estado == "COMPLETADO")
                .Select(c => new AsientoItem { Fecha = c.FechaCompra, Concepto = "Compra/Gasto", Referencia = $"Folio {c.CompraID}", Ingreso_Debe = 0, Egreso_Haber = c.TotalCompra })
                .ToList();

            return ventas.Concat(compras).OrderBy(x => x.Fecha).ToList();
        }

        // =================================================================
        // GENERACIÓN DE PDF MEJORADA Y ESTILIZADA
        // =================================================================
        public void GenerarPDF(string ruta, List<AsientoItem> datos, DateTime inicio, DateTime fin)
        {
            using PdfWriter writer = new PdfWriter(ruta);
            using PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Colores corporativos personalizados
            Color colorPrincipal = new DeviceRgb(44, 62, 80); // Azul oscuro elegante
            Color colorFilaPar = new DeviceRgb(236, 240, 241); // Gris muy claro (Efecto Cebra)
            Color colorBlanco = ColorConstants.WHITE;

            // 1. Encabezado Principal del Negocio
            Paragraph tituloNegocio = new Paragraph("MINISUPER MAYORGA")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(24)
                .SetFontColor(colorPrincipal);
            document.Add(tituloNegocio);

            // 2. Información del Reporte
            Paragraph subtitulo = new Paragraph("REPORTE: Libro Diario - Asiento Contable")
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER);
            document.Add(subtitulo);

            Paragraph infoPeriodo = new Paragraph()
                .Add(new Text("Generado el: "))
                .Add(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                .Add("\n")
                .Add(new Text("Periodo evaluado: "))
                .Add($"{inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy}")
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontSize(10)
                .SetMarginBottom(15);
            document.Add(infoPeriodo);

            // 3. Configuración de la Tabla (6 Columnas con anchos relativos)
            float[] anchosColumnas = { 1f, 2.5f, 4f, 3f, 2.5f, 2.5f };
            Table tabla = new Table(anchosColumnas).UseAllAvailableWidth();

            // Cabeceras de la tabla (¡CORREGIDO EL IN A TRAVÉS DE LA LISTA!)
            string[] cabeceras = { "Item", "Fecha", "Concepto", "Referencia", "Debe (Ingreso)", "Haber (Egreso)" };
            foreach (var cabecera in cabeceras)
            {
                Cell celda = new Cell()
                    .Add(new Paragraph(cabecera))
                    .SetBackgroundColor(colorPrincipal)
                    .SetFontColor(colorBlanco)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetPadding(5);
                tabla.AddHeaderCell(celda);
            }

            // 4. Llenar los datos con diseño intercalado (Cebra) y columna "Item"
            int itemContador = 1;
            decimal totalDebe = 0;
            decimal totalHaber = 0;

            foreach (var item in datos)
            {
                bool esPar = itemContador % 2 == 0;
                Color fondoCelda = esPar ? colorFilaPar : colorBlanco;

                // Celdas de la fila
                tabla.AddCell(new Cell().Add(new Paragraph(itemContador.ToString())).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Fecha.ToString("dd/MM/yyyy"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.CENTER));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Concepto)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Referencia)).SetBackgroundColor(fondoCelda));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Ingreso_Debe.ToString("C$ #,##0.00"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));
                tabla.AddCell(new Cell().Add(new Paragraph(item.Egreso_Haber.ToString("C$ #,##0.00"))).SetBackgroundColor(fondoCelda).SetTextAlignment(TextAlignment.RIGHT));

                totalDebe += item.Ingreso_Debe;
                totalHaber += item.Egreso_Haber;
                itemContador++;
            }

            // 5. Fila de Totales
            Cell celdaTotalTexto = new Cell(1, 4) // Ocupa 4 columnas
                .Add(new Paragraph("TOTALES:"))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBackgroundColor(colorPrincipal)
                .SetFontColor(colorBlanco)
                .SetPadding(5);

            Cell celdaTotalDebe = new Cell()
                .Add(new Paragraph(totalDebe.ToString("C$ #,##0.00")))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBackgroundColor(colorPrincipal)
                .SetFontColor(colorBlanco)
                .SetPadding(5);

            Cell celdaTotalHaber = new Cell()
                .Add(new Paragraph(totalHaber.ToString("C$ #,##0.00")))
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetBackgroundColor(colorPrincipal)
                .SetFontColor(colorBlanco)
                .SetPadding(5);

            tabla.AddCell(celdaTotalTexto);
            tabla.AddCell(celdaTotalDebe);
            tabla.AddCell(celdaTotalHaber);

            document.Add(tabla);

            // 6. Balance final
            decimal balance = totalDebe - totalHaber;
            Paragraph textoBalance = new Paragraph()
                .Add(new Text($"\nBALANCE DEL PERIODO: "))
                .Add(new Text(balance.ToString("C$ #,##0.00")))
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontColor(balance >= 0 ? new DeviceRgb(39, 174, 96) : new DeviceRgb(231, 76, 60)); // Verde si es positivo, rojo si es negativo

            document.Add(textoBalance);

            document.Close();
        }

        // =================================================================
        // GENERACIÓN DE EXCEL MEJORADA Y ESTILIZADA
        // =================================================================
        public void GenerarExcel(string ruta, List<AsientoItem> datos)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Asiento Contable");

            // 1. Título y Encabezado del Negocio
            var rangoTitulo = worksheet.Range("A1:F1");
            rangoTitulo.Merge().Value = "MINISUPER MAYORGA";
            rangoTitulo.Style.Font.Bold = true;
            rangoTitulo.Style.Font.FontSize = 22;
            rangoTitulo.Style.Font.FontColor = XLColor.White;
            rangoTitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            rangoTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rangoTitulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Row(1).Height = 35;

            var rangoSubtitulo = worksheet.Range("A2:F2");
            rangoSubtitulo.Merge().Value = "REPORTE: Libro Diario - Asiento Contable";
            rangoSubtitulo.Style.Font.Bold = true;
            rangoSubtitulo.Style.Font.FontSize = 14;
            rangoSubtitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell("A3").Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}";
            worksheet.Range("A3:F3").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // 2. Cabeceras de la Tabla (Inicia en la Fila 5)
            int filaInicio = 5;
            worksheet.Cell(filaInicio, 1).Value = "Item";
            worksheet.Cell(filaInicio, 2).Value = "Fecha";
            worksheet.Cell(filaInicio, 3).Value = "Concepto";
            worksheet.Cell(filaInicio, 4).Value = "Referencia";
            worksheet.Cell(filaInicio, 5).Value = "Debe (Ingreso)";
            worksheet.Cell(filaInicio, 6).Value = "Haber (Egreso)";

            var cabeceraEstilo = worksheet.Range(filaInicio, 1, filaInicio, 6).Style;
            cabeceraEstilo.Font.Bold = true;
            cabeceraEstilo.Font.FontColor = XLColor.White;
            cabeceraEstilo.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            cabeceraEstilo.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // 3. Llenado de Datos
            int filaActual = filaInicio + 1;
            int itemContador = 1;

            foreach (var item in datos)
            {
                worksheet.Cell(filaActual, 1).Value = itemContador;
                worksheet.Cell(filaActual, 2).Value = item.Fecha.ToString("dd/MM/yyyy");
                worksheet.Cell(filaActual, 3).Value = item.Concepto;
                worksheet.Cell(filaActual, 4).Value = item.Referencia;

                // Asignamos el valor como número para que Excel pueda sumar
                worksheet.Cell(filaActual, 5).Value = item.Ingreso_Debe;
                worksheet.Cell(filaActual, 6).Value = item.Egreso_Haber;

                filaActual++;
                itemContador++;
            }

            // 4. Formato de Bordes, Alineación y Moneda
            var rangoTabla = worksheet.Range(filaInicio, 1, filaActual - 1, 6);
            rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Centrar columnas 1 y 2
            worksheet.Range(filaInicio + 1, 1, filaActual - 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Formato de contabilidad (Moneda) para las columnas 5 y 6
            worksheet.Range(filaInicio + 1, 5, filaActual - 1, 6).Style.NumberFormat.Format = "$ #,##0.00";

            // 5. Fila de Totales
            worksheet.Range(filaActual, 1, filaActual, 4).Merge().Value = "TOTALES:";
            worksheet.Range(filaActual, 1, filaActual, 4).Style.Font.Bold = true;
            worksheet.Range(filaActual, 1, filaActual, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Range(filaActual, 1, filaActual, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECF0F1");

            // Fórmulas de Excel para sumar columnas
            worksheet.Cell(filaActual, 5).FormulaA1 = $"SUM(E{filaInicio + 1}:E{filaActual - 1})";
            worksheet.Cell(filaActual, 6).FormulaA1 = $"SUM(F{filaInicio + 1}:F{filaActual - 1})";

            worksheet.Cell(filaActual, 5).Style.Font.Bold = true;
            worksheet.Cell(filaActual, 6).Style.Font.Bold = true;
            worksheet.Cell(filaActual, 5).Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell(filaActual, 6).Style.NumberFormat.Format = "$ #,##0.00";

            // 6. Ajustar el tamaño de las columnas automáticamente
            worksheet.Columns().AdjustToContents();

            // Guardar
            workbook.SaveAs(ruta);
        }
    }
}