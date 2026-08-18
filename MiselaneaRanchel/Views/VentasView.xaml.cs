using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class VentasView : UserControl
    {
        private readonly ApplicationDbContext _context;

        // Colección observable para que el DataGrid se actualice automáticamente
        private ObservableCollection<DetalleVentaTemporal> _carrito;
        private decimal _totalVenta = 0;

        public VentasView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _carrito = new ObservableCollection<DetalleVentaTemporal>();
            DgCarrito.ItemsSource = _carrito;

            // Al cargar la vista, el cursor se pone directo en el buscador
            TxtBuscador.Focus();
        }

        // =================================================================
        // 1. EVENTOS DEL BUSCADOR Y AUTOCOMPLETADO (POPUP)
        // =================================================================

        private void TxtBuscador_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PopupSugerencias.IsOpen = false; // Cerramos sugerencias si damos enter
                BuscarYAgregarProducto();
            }
            else if (e.Key == Key.Down && PopupSugerencias.IsOpen)
            {
                // Mover el foco a la lista desplegable para navegar con teclado
                LstSugerencias.Focus();
                if (LstSugerencias.Items.Count > 0)
                    LstSugerencias.SelectedIndex = 0;
            }
        }

        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = TxtBuscador.Text.Trim();

            // Evitar buscar si el campo está vacío
            if (string.IsNullOrEmpty(busqueda))
            {
                PopupSugerencias.IsOpen = false;
                return;
            }

            // Buscar coincidencias en BD (.Take(10) evita que el sistema se trabe si hay miles de productos)
            var coincidencias = _context.Productos
                .Where(p => p.Activo == true &&
                           (p.Descripcion.Contains(busqueda) || p.CodigoBarras.Contains(busqueda)))
                .Take(10)
                .ToList();

            if (coincidencias.Any())
            {
                LstSugerencias.ItemsSource = coincidencias;
                PopupSugerencias.IsOpen = true;
            }
            else
            {
                PopupSugerencias.IsOpen = false;
            }
        }

        private void LstSugerencias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Producto productoSeleccionado)
            {
                ProcesarSeleccionPopup(productoSeleccionado);
            }
        }

        private void LstSugerencias_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LstSugerencias.SelectedItem is Producto productoSeleccionado)
            {
                ProcesarSeleccionPopup(productoSeleccionado);
            }
        }

        private void ProcesarSeleccionPopup(Producto productoSeleccionado)
        {
            PopupSugerencias.IsOpen = false;

            // Desuscribimos temporalmente el TextChanged para que no vuelva a abrir el popup al setear el texto
            TxtBuscador.TextChanged -= TxtBuscador_TextChanged;

            // Ponemos el código de barras exacto en el textbox
            TxtBuscador.Text = productoSeleccionado.CodigoBarras;

            TxtBuscador.TextChanged += TxtBuscador_TextChanged;

            // Ejecutamos tu lógica existente de búsqueda y agregado
            BuscarYAgregarProducto();

            // Reseteamos el listbox
            LstSugerencias.SelectedItem = null;
            TxtBuscador.Focus();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarYAgregarProducto();
        }

        // =================================================================
        // 2. LÓGICA PRINCIPAL: AGREGAR AL CARRITO
        // =================================================================
        private void BuscarYAgregarProducto()
        {
            string busqueda = TxtBuscador.Text.Trim();
            if (string.IsNullOrEmpty(busqueda)) return;

            // Busca el producto por código de barras o nombre
            var producto = _context.Productos
                .FirstOrDefault(p => (p.CodigoBarras == busqueda || p.Descripcion == busqueda) && p.Activo == true);

            if (producto != null)
            {
                // Verifica si hay stock
                if (producto.StockActual <= 0)
                {
                    MessageBox.Show($"El producto '{producto.Descripcion}' no tiene stock disponible (0).", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtBuscador.SelectAll();
                    return;
                }

                // Verifica si ya está en el carrito
                var itemEnCarrito = _carrito.FirstOrDefault(c => c.ProductoID == producto.ProductoID);

                if (itemEnCarrito != null)
                {
                    if ((itemEnCarrito.Cantidad + 1) > producto.StockActual)
                    {
                        MessageBox.Show($"Stock insuficiente. Solo quedan {producto.StockActual} disponibles.", "Stock Bajo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        itemEnCarrito.Cantidad += 1;
                        DgCarrito.Items.Refresh(); // Refresca la vista de la tabla
                    }
                }
                else
                {
                    // Si es nuevo, lo agregamos al carrito
                    _carrito.Add(new DetalleVentaTemporal
                    {
                        ProductoID = producto.ProductoID,
                        Codigo = producto.CodigoBarras,
                        Descripcion = producto.Descripcion,
                        PrecioCosto = producto.PrecioCosto,
                        PrecioVenta = producto.PrecioVenta,
                        Cantidad = 1
                    });
                }

                ActualizarTotales();

                // Limpia el buscador para el siguiente producto
                TxtBuscador.Text = "";
                TxtBuscador.Focus();
            }
            else
            {
                MessageBox.Show("Producto no encontrado.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtBuscador.SelectAll();
            }
        }

        // =================================================================
        // 3. CÁLCULO DE TOTALES Y CAMBIO
        // =================================================================
        private void ActualizarTotales()
        {
            _totalVenta = _carrito.Sum(x => x.SubTotal);

            TxtTotalPagar.Text = _totalVenta.ToString("C2");
            TxtTotalArticulos.Text = _carrito.Sum(x => x.Cantidad).ToString("N0");

            CalcularCambio();
        }

        private void TxtEfectivo_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularCambio();
        }

        private void CalcularCambio()
        {
            if (decimal.TryParse(TxtEfectivo.Text, out decimal efectivoRecibido))
            {
                decimal cambio = efectivoRecibido - _totalVenta;

                if (cambio < 0)
                {
                    TxtCambio.Text = "$ 0.00";
                }
                else
                {
                    TxtCambio.Text = cambio.ToString("C2");
                }
            }
            else
            {
                TxtCambio.Text = "$ 0.00";
            }
        }

        // =================================================================
        // 4. PROCESAR VENTA (GUARDAR EN BD Y DESCONTAR INVENTARIO)
        // =================================================================
        private void BtnCobrar_Click(object sender, RoutedEventArgs e)
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("El carrito de compras está vacío.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal.TryParse(TxtEfectivo.Text, out decimal efectivo);
            decimal cambio = efectivo - _totalVenta;

            if (efectivo < _totalVenta)
            {
                MessageBox.Show("El efectivo recibido es menor al total a pagar.", "Falta Dinero", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtEfectivo.Focus();
                return;
            }

            using (var transaccion = _context.Database.BeginTransaction())
            {
                try
                {
                    var nuevaVenta = new Venta
                    {
                        NumeroTicket = $"TCK-{DateTime.Now:yyyyMMddHHmmss}",
                        FechaVenta = DateTime.Now,
                        TotalVenta = _totalVenta,
                        EfectivoRecibido = efectivo,
                        CambioEntregado = cambio,
                        Estado = "COMPLETADO"
                    };

                    _context.Ventas.Add(nuevaVenta);
                    _context.SaveChanges();

                    foreach (var item in _carrito)
                    {
                        var detalle = new DetalleVenta
                        {
                            VentaID = nuevaVenta.VentaID,
                            ProductoID = item.ProductoID,
                            Cantidad = item.Cantidad,
                            PrecioCostoHistorico = item.PrecioCosto,
                            PrecioVentaHistorico = item.PrecioVenta,
                            SubTotal = item.SubTotal
                        };
                        _context.DetalleVentas.Add(detalle);

                        var productoBd = _context.Productos.Find(item.ProductoID);
                        if (productoBd != null)
                        {
                            productoBd.StockActual -= item.Cantidad;
                            _context.Productos.Update(productoBd);
                        }

                        var movimiento = new MovimientoInventario
                        {
                            ProductoID = item.ProductoID,
                            TipoMovimiento = "SALIDA",
                            Cantidad = item.Cantidad,
                            FechaMovimiento = DateTime.Now,
                            Motivo = $"Venta POS - Ticket: {nuevaVenta.NumeroTicket}",
                            ReferenciaID = nuevaVenta.VentaID
                        };
                        _context.MovimientosInventario.Add(movimiento);
                    }

                    _context.SaveChanges();
                    transaccion.Commit();

                    MessageBox.Show($"¡Venta cobrada con éxito!\n\nSu Cambio es: {cambio:C2}", "Cobro Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);

                    LimpiarPantallaVenta();
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    MessageBox.Show($"Error al cobrar: {ex.Message}", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // =================================================================
        // 5. CANCELAR Y LIMPIAR
        // =================================================================
        private void BtnCancelarVenta_Click(object sender, RoutedEventArgs e)
        {
            if (_carrito.Count > 0)
            {
                var result = MessageBox.Show("¿Estás seguro de cancelar toda la venta?", "Confirmar Cancelación", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    LimpiarPantallaVenta();
                }
            }
        }

        private void LimpiarPantallaVenta()
        {
            _carrito.Clear();
            _totalVenta = 0;
            TxtBuscador.Text = "";
            TxtEfectivo.Text = "";
            TxtCambio.Text = "$ 0.00";
            ActualizarTotales();

            TxtBuscador.Focus();
        }
    }

    // =================================================================
    // CLASE AUXILIAR PARA LA GRILLA
    // =================================================================
    public class DetalleVentaTemporal
    {
        public int ProductoID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal PrecioCosto { get; set; }

        private decimal _cantidad;
        public decimal Cantidad
        {
            get => _cantidad;
            set { _cantidad = value; }
        }

        public decimal SubTotal => Cantidad * PrecioVenta;
    }
}