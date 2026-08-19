using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using MiselaneaRanchel.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.Kernel.Colors;

namespace MiselaneaRanchel.Reportes
{
    public class GeneradorTicket
    {
        public void CrearEImprimirTicket(Venta venta, List<DetalleVentaTemporal> carritoTicket, decimal efectivo, decimal cambio)
        {
            try
            {
                // 1. Crear la ruta de guardado
                string rutaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string carpetaTickets = System.IO.Path.Combine(rutaDocumentos, "Tickets_MiniSuper");

                if (!Directory.Exists(carpetaTickets))
                    Directory.CreateDirectory(carpetaTickets);

                string rutaArchivo = System.IO.Path.Combine(carpetaTickets, $"{venta.NumeroTicket}.pdf");

                // 2. Inicializar iText (Tamaño A6 para impresoras térmicas/recibos)
                using PdfWriter writer = new PdfWriter(rutaArchivo);
                using PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf, PageSize.A6);
                document.SetMargins(15, 15, 15, 15);

                // 3. ENCABEZADO
                Paragraph titulo = new Paragraph("MINISUPER RM");
                titulo.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                titulo.SetProperty(Property.FONT_WEIGHT, 700);
                titulo.SetFontSize(14);
                document.Add(titulo);

                Paragraph subtitulo = new Paragraph("Ticket de Compra");
                subtitulo.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                subtitulo.SetFontSize(10);
                subtitulo.SetMarginBottom(10);
                document.Add(subtitulo);

                Paragraph info = new Paragraph($"Fecha: {venta.FechaVenta:dd/MM/yyyy HH:mm}\nTicket: {venta.NumeroTicket}");
                info.SetTextAlignment(iText.Layout.Properties.TextAlignment.LEFT);
                info.SetFontSize(9);
                document.Add(info);

                Paragraph linea = new Paragraph("---------------------------------------------------------");
                linea.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                document.Add(linea);

                // 4. TABLA DE PRODUCTOS
                Table tabla = new Table(new float[] { 1f, 3f, 1.5f }).UseAllAvailableWidth();

                // Cabeceras
                Cell celdaCant = new Cell().Add(new Paragraph("Cant"));
                celdaCant.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                celdaCant.SetProperty(Property.FONT_WEIGHT, 700);
                celdaCant.SetFontSize(9);
                tabla.AddHeaderCell(celdaCant);

                Cell celdaDesc = new Cell().Add(new Paragraph("Descripción"));
                celdaDesc.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                celdaDesc.SetProperty(Property.FONT_WEIGHT, 700);
                celdaDesc.SetFontSize(9);
                tabla.AddHeaderCell(celdaDesc);

                Cell celdaImporte = new Cell().Add(new Paragraph("Importe"));
                celdaImporte.SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                celdaImporte.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                celdaImporte.SetProperty(Property.FONT_WEIGHT, 700);
                celdaImporte.SetFontSize(9);
                tabla.AddHeaderCell(celdaImporte);

                // Filas de productos
                foreach (var item in carritoTicket)
                {
                    Cell cCant = new Cell().Add(new Paragraph(item.Cantidad.ToString("N2")));
                    cCant.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                    cCant.SetFontSize(8);
                    tabla.AddCell(cCant);

                    Cell cDesc = new Cell().Add(new Paragraph(item.Descripcion));
                    cDesc.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                    cDesc.SetFontSize(8);
                    tabla.AddCell(cDesc);

                    Cell cImporte = new Cell().Add(new Paragraph(item.SubTotal.ToString("C$ #,##0.00")));
                    cImporte.SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                    cImporte.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                    cImporte.SetFontSize(8);
                    tabla.AddCell(cImporte);
                }
                document.Add(tabla);

                Paragraph linea2 = new Paragraph("---------------------------------------------------------");
                linea2.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                document.Add(linea2);

                // 5. TOTALES
                Paragraph total = new Paragraph($"TOTAL: {venta.TotalVenta:C$ #,##0.00}");
                total.SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                total.SetProperty(Property.FONT_WEIGHT, 700);
                total.SetFontSize(12);
                document.Add(total);

                Paragraph efectivoCambio = new Paragraph($"Efectivo: {efectivo:C$ #,##0.00}\nCambio: {cambio:C$ #,##0.00}");
                efectivoCambio.SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);
                efectivoCambio.SetFontSize(9);
                document.Add(efectivoCambio);

                // 6. PIE DE PÁGINA
                Paragraph pie = new Paragraph("\n¡Gracias por su compra!\nVuelva pronto.");
                pie.SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                pie.SetProperty(Property.FONT_WEIGHT, 700);
                pie.SetFontSize(10);
                document.Add(pie);

                document.Close();

                // 7. Abrir el PDF automáticamente
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"La venta se guardó correctamente, pero hubo un error al generar el ticket en PDF.\nError: {ex.Message}", "Aviso de Ticket", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}