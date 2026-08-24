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

        // Variables para la matemática interna (ya no necesitamos el fondoInicial aquí)
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

                // Plasmar los números de la BD en las tarjetas
                TxtVentasEfectivo.Text = _ventasDelDia.ToString("C$ #,##0.00");
                TxtSalidasExtra.Text = $"- {_salidasDelDia.ToString("C$ #,##0.00")}";

                // Refrescamos la matemática sumando el fondo
                RecalcularTotales();
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
        // 2. LÓGICA EN TIEMPO REAL (FONDOS, DIFERENCIAS Y COLORES)
        // ====================================================================

        // Se ejecuta si el usuario cambia el valor del Fondo Inicial manualmente
        private void TxtFondoInicial_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalcularTotales();
        }

        private void RecalcularTotales()
        {
            // ESCUDO: Si la pantalla apenas está cargando y estos textos no existen, no hacemos nada aún.
            if (TxtTotalEsperado == null || TxtFondoInicial == null) return;

            // Leer el fondo de la pantalla (Si está vacío, se asume 0)
            if (!decimal.TryParse(TxtFondoInicial.Text, out decimal fondoInicial))
            {
                fondoInicial = 0;
            }

            // Calcular el Total Esperado
            _totalEsperado = fondoInicial + _ventasDelDia - _salidasDelDia;
            TxtTotalEsperado.Text = _totalEsperado.ToString("C$ #,##0.00");

            // Recalcular también la diferencia con la gaveta
            ActualizarDiferencia();
        }

        private void TxtEfectivoReal_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarDiferencia();
        }

        private void ActualizarDiferencia()
        {
            // ESCUDO: Validamos que los controles gráficos ya existan en la pantalla
            if (TxtDiferencia == null || TxtEstadoCaja == null || TxtEfectivoReal == null) return;

            if (decimal.TryParse(TxtEfectivoReal.Text, out decimal efectivoFisico))
            {
                decimal diferencia = efectivoFisico - _totalEsperado;

                // Mostrar la diferencia con formato de moneda
                TxtDiferencia.Text = diferencia.ToString("C$ #,##0.00");

                // Lógica de colores y estados
                if (diferencia == 0)
                {
                    // Caja Cuadrada
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                    TxtEstadoCaja.Text = "CAJA CUADRADA";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                }
                else if (diferencia < 0)
                {
                    // Faltante de dinero
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    TxtEstadoCaja.Text = "FALTANTE DE DINERO";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                }
                else
                {
                    // Sobrante de dinero
                    TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                    TxtEstadoCaja.Text = "SOBRANTE DE DINERO";
                    TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));
                }
            }
            else
            {
                // Si el campo está vacío o tiene letras
                TxtDiferencia.Text = "C$ 0.00";
                TxtDiferencia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2C3E50"));
                TxtEstadoCaja.Text = "ESPERANDO CONTEO";
                TxtEstadoCaja.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7"));
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

            if (!decimal.TryParse(TxtFondoInicial.Text, out decimal fondoInicialFinal))
            {
                fondoInicialFinal = 0;
            }

            decimal diferencia = efectivoFisico - _totalEsperado;

            // Confirmación de seguridad
            var confirmacion = MessageBox.Show(
                $"Vas a cerrar el turno con los siguientes datos:\n\n" +
                $"Fondo Inicial: {fondoInicialFinal.ToString("C$ #,##0.00")}\n" +
                $"Total Esperado: {_totalEsperado.ToString("C$ #,##0.00")}\n" +
                $"Total Físico: {efectivoFisico.ToString("C$ #,##0.00")}\n" +
                $"Diferencia: {diferencia.ToString("C$ #,##0.00")}\n\n" +
                "¿Estás seguro de proceder?", "Confirmar Cierre", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirmacion == MessageBoxResult.Yes)
            {
                try
                {
                    // Guardar en la Base de Datos
                    var nuevoCorte = new CorteDeCaja
                    {
                        FechaCorte = DateTime.Now,
                        FondoInicial = fondoInicialFinal,
                        VentasEfectivo = _ventasDelDia,
                        SalidasExtra = _salidasDelDia,
                        TotalEsperado = _totalEsperado,
                        EfectivoReal = efectivoFisico,
                        Diferencia = diferencia,
                        EstadoCaja = diferencia == 0 ? "CUADRADA" : (diferencia < 0 ? "FALTANTE" : "SOBRANTE")
                    };

                    _context.CortesDeCaja.Add(nuevoCorte);
                    _context.SaveChanges();

                    MessageBox.Show("¡Corte de caja guardado con éxito! El turno ha sido cerrado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Bloqueamos la pantalla después de cerrar
                    TxtFondoInicial.IsReadOnly = true;
                    TxtEfectivoReal.IsReadOnly = true;
                    BtnCerrarTurno.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ocurrió un error al guardar el corte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =================================================================
        // VALIDACIONES DE ENTRADA (SOLO NÚMEROS Y DECIMALES)
        // =================================================================

        private void ValidarSoloNumerosDecimales(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;

            // Permitir solo dígitos y un único punto decimal
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
            {
                e.Handled = true;
            }
            // Si el usuario presionó un punto, verificamos que no exista ya uno en la caja de texto
            else if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }

        private void PrevenirPegadoInvalido(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string textoPegado = (string)e.DataObject.GetData(typeof(string));

                if (!decimal.TryParse(textoPegado, out _))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}