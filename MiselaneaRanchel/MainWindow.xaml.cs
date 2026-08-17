using System.Windows;

namespace MiselaneaRanchel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Cargar el Dashboard por defecto al iniciar el sistema
            MainContentControl.Content = new Views.DashboardView();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Dashboard
            MainContentControl.Content = new Views.DashboardView();
        }

        private void BtnVentas_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Ventas
            MainContentControl.Content = new Views.VentasView();
        }

        private void BtnInventario_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Inventario
            MainContentControl.Content = new Views.InventarioView();
        }

        private void BtnCompras_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Compras
            MainContentControl.Content = new Views.ComprasView();
        }

        private void BtnCaja_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Corte de Caja
            MainContentControl.Content = new Views.CajaView();
        }

        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Reportes
            MainContentControl.Content = new Views.ReportesView();
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a Configuración
            MainContentControl.Content = new Views.ConfiguracionView();
        }

        private void BtnCatalogos_Click(object sender, RoutedEventArgs e)
        {
            // Cambiar la vista a la pantalla de Catálogos
            MainContentControl.Content = new Views.CatalogosView();
        }
    }
}