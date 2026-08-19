using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class DetalleCompraWindow : Window
    {
        private readonly ApplicationDbContext _context;

        // Recibimos la Compra que el usuario seleccionó en la grilla
        public DetalleCompraWindow(Compra compraSeleccionada)
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            CargarDetalles(compraSeleccionada);
        }

        private void CargarDetalles(Compra compra)
        {
            try
            {
                // Llenamos la cabecera (TextBlocks)
                TxtProveedor.Text = compra.Proveedor?.Nombre ?? "Proveedor Desconocido";
                TxtFactura.Text = compra.NumeroFactura;
                TxtFecha.Text = compra.FechaCompra.ToString("dd/MM/yyyy HH:mm");
                TxtTotalFactura.Text = compra.TotalCompra.ToString("C2");

                // Buscamos los detalles en la base de datos unidos con la tabla Productos
                var detalles = _context.DetalleCompras
                                       .Include(d => d.Producto)
                                       .Where(d => d.CompraID == compra.CompraID)
                                       .ToList();

                // Pasamos los datos al DataGrid
                DgDetalles.ItemsSource = detalles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los detalles: {ex.Message}");
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra la ventana pop-up
        }
    }
}