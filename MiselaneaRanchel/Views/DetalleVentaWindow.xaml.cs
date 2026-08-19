using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class DetalleVentaWindow : Window
    {
        private readonly ApplicationDbContext _context;

        public DetalleVentaWindow(Venta ventaSeleccionada)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            CargarDetalles(ventaSeleccionada);
        }

        private void CargarDetalles(Venta venta)
        {
            try
            {
                // Llenamos la cabecera (TextBlocks)
                TxtTicket.Text = venta.NumeroTicket;
                TxtFecha.Text = venta.FechaVenta.ToString("dd/MM/yyyy HH:mm");
                TxtTotalFactura.Text = venta.TotalVenta.ToString("C$ #,##0.00");

                // Buscamos los detalles en la base de datos unidos con la tabla Productos
                var detalles = _context.DetalleVentas
                                       .Include(d => d.Producto)
                                       .Where(d => d.VentaID == venta.VentaID)
                                       .ToList();

                // Pasamos los datos al DataGrid
                DgDetalles.ItemsSource = detalles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los detalles de la venta: {ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra el popup
        }
    }
}