using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Reportes;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class ReportesView : UserControl
    {
        private readonly ApplicationDbContext _context;

        private string _reporteActivo = "";
        private object _datosActuales;

        public ReportesView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            DpDesde.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DpHasta.SelectedDate = DateTime.Now;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await PdfViewer.EnsureCoreWebView2Async(null);
        }

        private (DateTime Inicio, DateTime Fin) ObtenerFechas()
        {
            DateTime inicio = DpDesde.SelectedDate ?? DateTime.Today;
            DateTime fin = DpHasta.SelectedDate ?? DateTime.Today;
            return (inicio, fin.Date.AddDays(1).AddTicks(-1));
        }

        private void MostrarPdfEnVisor(string rutaTemporal)
        {
            if (PdfViewer.CoreWebView2 != null)
                PdfViewer.CoreWebView2.Navigate(rutaTemporal);
        }

        // ===============================================
        // BOTONES DE CONTABILIDAD
        // ===============================================
        private void BtnAsientoContable_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "Asiento";
            var fechas = ObtenerFechas();
            var reporte = new ReporteAsientoContable();
            var datos = reporte.ObtenerDatos(_context, fechas.Inicio, fechas.Fin);
            _datosActuales = datos;

            TxtTituloReporte.Text = $"Vista Previa: Libro Diario ({fechas.Inicio:dd/MM/yyyy} al {fechas.Fin:dd/MM/yyyy})";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Asiento_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, fechas.Inicio, fechas.Fin);
            MostrarPdfEnVisor(rutaTemporal);
        }

        private void BtnEstadoResultados_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "Resultados";
            var fechas = ObtenerFechas();
            var reporte = new ReporteEstadoResultados();
            var datos = reporte.ObtenerDatos(_context, fechas.Inicio, fechas.Fin, out decimal utilidadNeta);
            _datosActuales = datos;

            TxtTituloReporte.Text = "Vista Previa: Estado de Resultados";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Resultados_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, fechas.Inicio, fechas.Fin);
            MostrarPdfEnVisor(rutaTemporal);
        }

        private void BtnBalanceGeneral_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "Balance";
            var fechas = ObtenerFechas();
            var reporte = new ReporteBalanceGeneral();
            var datos = reporte.ObtenerDatos(_context, fechas.Fin, out decimal totalActivos);
            _datosActuales = datos;

            TxtTituloReporte.Text = $"Vista Previa: Balance General al {fechas.Fin:dd/MM/yyyy}";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Balance_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, fechas.Fin);
            MostrarPdfEnVisor(rutaTemporal);
        }

        // ===============================================
        // BOTONES DE INVENTARIO
        // ===============================================
        private void BtnStock_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "Stock";
            var reporte = new ReporteStock();
            var datos = reporte.ObtenerDatos(_context);
            _datosActuales = datos;

            TxtTituloReporte.Text = "Vista Previa: Stock Actual en Bodega";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Stock_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, DateTime.Now);
            MostrarPdfEnVisor(rutaTemporal);
        }

        private void BtnKardex_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "Kardex";
            var fechas = ObtenerFechas();
            var reporte = new ReporteKardex();
            var datos = reporte.ObtenerDatos(_context, fechas.Inicio, fechas.Fin);
            _datosActuales = datos;

            TxtTituloReporte.Text = $"Vista Previa: Kardex ({fechas.Inicio:dd/MM/yyyy} al {fechas.Fin:dd/MM/yyyy})";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Kardex_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, fechas.Inicio, fechas.Fin);
            MostrarPdfEnVisor(rutaTemporal);
        }

        // ===============================================
        // BOTONES DE CAJA
        // ===============================================
        private void BtnCierreCaja_Click(object sender, RoutedEventArgs e)
        {
            _reporteActivo = "CierreCaja";
            var fechas = ObtenerFechas(); // Se usa solo "Hasta" como el día a evaluar
            var reporte = new ReporteCierreCaja();
            var datos = reporte.ObtenerDatos(_context, fechas.Fin);
            _datosActuales = datos;

            TxtTituloReporte.Text = $"Vista Previa: Cierre de Caja ({fechas.Fin:dd/MM/yyyy})";
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"Preview_Cierre_{Guid.NewGuid()}.pdf");
            reporte.GenerarPDF(rutaTemporal, datos, fechas.Fin);
            MostrarPdfEnVisor(rutaTemporal);
        }

        // ===============================================
        // EXPORTACIONES
        // ===============================================
        private void BtnExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_reporteActivo) || _datosActuales == null)
            {
                MessageBox.Show("Por favor, genera la vista previa de un reporte primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"Reporte_{_reporteActivo}_{DateTime.Now:ddMMyy}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var fechas = ObtenerFechas();

                    if (_reporteActivo == "Asiento") new ReporteAsientoContable().GenerarPDF(dialog.FileName, (List<AsientoItem>)_datosActuales, fechas.Inicio, fechas.Fin);
                    else if (_reporteActivo == "Resultados") new ReporteEstadoResultados().GenerarPDF(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales, fechas.Inicio, fechas.Fin);
                    else if (_reporteActivo == "Balance") new ReporteBalanceGeneral().GenerarPDF(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales, fechas.Fin);
                    else if (_reporteActivo == "Stock") new ReporteStock().GenerarPDF(dialog.FileName, (List<ItemStock>)_datosActuales, DateTime.Now);
                    else if (_reporteActivo == "Kardex") new ReporteKardex().GenerarPDF(dialog.FileName, (List<ItemKardex>)_datosActuales, fechas.Inicio, fechas.Fin);
                    else if (_reporteActivo == "CierreCaja") new ReporteCierreCaja().GenerarPDF(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales, fechas.Fin);

                    MessageBox.Show("Documento PDF guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al guardar PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_reporteActivo) || _datosActuales == null)
            {
                MessageBox.Show("Por favor, genera la vista previa de un reporte primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Reporte_{_reporteActivo}_{DateTime.Now:ddMMyy}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var fechas = ObtenerFechas();

                    if (_reporteActivo == "Asiento") new ReporteAsientoContable().GenerarExcel(dialog.FileName, (List<AsientoItem>)_datosActuales);
                    else if (_reporteActivo == "Resultados") new ReporteEstadoResultados().GenerarExcel(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales);
                    else if (_reporteActivo == "Balance") new ReporteBalanceGeneral().GenerarExcel(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales);
                    else if (_reporteActivo == "Stock") new ReporteStock().GenerarExcel(dialog.FileName, (List<ItemStock>)_datosActuales);
                    else if (_reporteActivo == "Kardex") new ReporteKardex().GenerarExcel(dialog.FileName, (List<ItemKardex>)_datosActuales);
                    else if (_reporteActivo == "CierreCaja") new ReporteCierreCaja().GenerarExcel(dialog.FileName, (List<ReporteItemFinanciero>)_datosActuales, fechas.Fin);

                    MessageBox.Show("Documento de Excel guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar a Excel: {ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}