using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class CajaView : UserControl
    {
        private readonly ApplicationDbContext _context;

        // Variables para la matemática interna
        private decimal _fondoInicial = 500.00m; // Puedes volver esto dinámico si creas una configuración
        private decimal _ventasDelDia = 0;
        private decimal _salidasDelDia = 0;
        private decimal _totalEsperado = 0;

        public CajaView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            CargarDatosDelDia();
        }

        // ====================================================================
        // 1. CARGAR DATOS DESDE LA BASE DE DATOS
        // ====================================================================
        private void CargarDatosDelDia()
        {
            try
            {
                DateTime hoy = DateTime.Today;

                // Sumar todas las VENTAS del día de hoy
                _ventasDelDia = _context.Ventas
                    .Where(v => v.FechaVenta.Date == hoy && v.Estado == "COMPLETADO")
                    .Sum(v => v.TotalVenta);

                // Sumar todas las COMPRAS (Salidas de dinero) del día de hoy
                _salidasDelDia = _context.Compras
                    .Where(c => c.FechaCompra.Date == hoy && c.Estado == "COMPLETADO")
                    .Sum(c => c.TotalCompra);

                // Calcular el Total Esperado
                _totalEsperado = _fondoInicial + _ventasDelDia - _salidasDelDia;

                // Plasmar los números en las tarjetas
                TxtFondoInicial.Text = _fondoInicial.ToString("C2");
                TxtVentasEfectivo.Text = _ventasDelDia.ToString("C2");
                TxtSalidasExtra.Text = $"- {_salidasDelDia.ToString("C2")}";
                TxtTotalEsperado.Text = _totalEsperado.ToString("C2");

                // Resetear la vista inferior
                TxtEfectivoReal.Text = "";
                ActualizarDiferencia();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos del día: {ex.Message}", "Error de Base de Datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefrescar_Click(object sender, RoutedEventArgs e)
        {
            CargarDatosDelDia();
        }

        // ====================================================================
        // 2. LÓGICA EN TIEMPO REAL (DIFERENCIAS Y COLORES)
        // ====================================================================
        private void TxtEfectivoReal_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarDiferencia();
        }

        private void ActualizarDiferencia()
        {
            if (decimal.TryParse(TxtEfectivoReal.Text, out decimal efectivoFisico))
            {
                decimal diferencia = efectivoFisico - _totalEsperado;

                // Mostrar la diferencia con formato de moneda
                TxtDiferencia.Text = diferencia.ToString("C2");

                // Lógica de colores y estados
                if (diferencia == 0)
                {
                    // Caja Cuadrada
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50")); // Gris Oscuro
                    TxtEstadoCaja.Text = "CAJA CUADRADA";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")); // Verde
                }
                else if (diferencia < 0)
                {
                    // Faltante de dinero
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // Rojo
                    TxtEstadoCaja.Text = "FALTANTE DE DINERO";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C")); // Rojo
                }
                else
                {
                    // Sobrante de dinero
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // Amarillo/Naranja
                    TxtEstadoCaja.Text = "SOBRANTE DE DINERO";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12")); // Amarillo/Naranja
                }
            }
            else
            {
                // Si el campo está vacío o tiene letras
                TxtDiferencia.Text = "$ 0.00";
                TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                TxtEstadoCaja.Text = "ESPERANDO CONTEO";
                TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7")); // Gris claro
            }
        }

        // ====================================================================
        // 3. GUARDAR EL CORTE DE CAJA EN LA BD
        // ====================================================================
        private void BtnCerrarTurno_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(TxtEfectivoReal.Text, out decimal efectivoFisico))
            {
                MessageBox.Show("Ingresa una cantidad válida en 'Efectivo Real Contado' antes de cerrar el turno.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEfectivoReal.Focus();
                return;
            }

            decimal diferencia = efectivoFisico - _totalEsperado;

            // Confirmación de seguridad
            var confirmacion = MessageBox.Show(
                $"Vas a cerrar el turno con los siguientes datos:\n\n" +
                $"Total Esperado: {_totalEsperado.ToString("C2")}\n" +
                $"Total Físico: {efectivoFisico.ToString("C2")}\n" +
                $"Diferencia: {diferencia.ToString("C2")}\n\n" +
                "¿Estás seguro de proceder?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmacion == MessageBoxResult.Yes)
            {
                try
                {
                    // Guardar en la Base de Datos
                    var nuevoCorte = new CorteDeCaja
                    {
                        FechaCorte = DateTime.Now,
                        FondoInicial = _fondoInicial,
                        VentasEfectivo = _ventasDelDia,
                        SalidasExtra = _salidasDelDia,
                        TotalEsperado = _totalEsperado,
                        EfectivoReal = efectivoFisico,
                        Diferencia = diferencia,

                        // Asignamos la lógica de la diferencia al campo EstadoCaja que tú creaste
                        EstadoCaja = diferencia == 0 ? "CUADRADA" : (diferencia < 0 ? "FALTANTE" : "SOBRANTE")
                    };

                    _context.CortesDeCaja.Add(nuevoCorte);
                    _context.SaveChanges();

                    MessageBox.Show("¡Corte de caja guardado con éxito! El turno ha sido cerrado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Opcional: Bloquear la pantalla después de cerrar
                    TxtEfectivoReal.IsReadOnly = true;
                    BtnCerrarTurno.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al guardar el corte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}