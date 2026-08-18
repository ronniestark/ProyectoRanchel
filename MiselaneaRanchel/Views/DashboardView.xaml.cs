using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly ApplicationDbContext _context;

        public DashboardView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
        }

        // Evento que se dispara automáticamente cuando la pantalla carga
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarEstadisticas();
        }

        // Botón para refrescar datos manualmente
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarEstadisticas();
        }

        private void CargarEstadisticas()
        {
            try
            {
                DateTime hoy = DateTime.Today;

                // 1. Obtener todas las VENTAS de HOY completadas
                var ventasHoy = _context.Ventas
                    .Where(v => v.FechaVenta.Date == hoy && v.Estado == "COMPLETADO")
                    .ToList();

                // 2. Obtener los DETALLES de las ventas de hoy para calcular ganancias y artículos
                var detallesHoy = _context.DetalleVentas
                    .Include(d => d.Producto)
                    .Include(d => d.Venta)
                    .Where(d => d.Venta.FechaVenta.Date == hoy && d.Venta.Estado == "COMPLETADO")
                    .ToList();

                // 3. Cálculos de las Tarjetas Superiores
                decimal totalVentas = ventasHoy.Sum(v => v.TotalVenta);
                decimal totalGanancia = detallesHoy.Sum(d => (d.PrecioVentaHistorico - d.PrecioCostoHistorico) * d.Cantidad);
                decimal totalArticulos = detallesHoy.Sum(d => d.Cantidad);
                int totalTickets = ventasHoy.Count;

                TxtVentasHoy.Text = totalVentas.ToString("C2");
                TxtGananciaHoy.Text = totalGanancia.ToString("C2");
                TxtArticulosVendidos.Text = totalArticulos.ToString("N0");
                TxtTicketsHoy.Text = totalTickets.ToString("N0");

                // 4. Cálculos del panel de Alertas (Gastos y Stock)
                decimal totalGastos = _context.Compras
                    .Where(c => c.FechaCompra.Date == hoy && c.Estado == "COMPLETADO")
                    .Sum(c => (decimal?)c.TotalCompra) ?? 0;

                TxtGastosHoy.Text = totalGastos.ToString("C2");

                // Productos con 5 o menos en stock (Modifica el 5 si tienes una propiedad "StockMinimo")
                int bajoStock = _context.Productos.Count(p => p.StockActual <= 5 && p.Activo == true);
                TxtBajoStock.Text = $"{bajoStock} producto(s)";

                // 5. TOP 5 Productos más vendidos hoy
                var topProductos = detallesHoy
                    .GroupBy(d => new { d.ProductoID, d.Producto.Descripcion })
                    .Select(g => new TopProductoViewModel
                    {
                        Descripcion = g.Key.Descripcion,
                        TotalVendido = g.Sum(x => x.Cantidad),
                        TotalDinero = g.Sum(x => x.SubTotal)
                    })
                    .OrderByDescending(x => x.TotalVendido)
                    .Take(5)
                    .ToList();

                DgTopProductos.ItemsSource = topProductos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las estadísticas del Dashboard: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

 
}