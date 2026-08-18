using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;
using MiselaneaRanchel.Reportes; 

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

            TxtBuscador.Focus();
        }

        // =================================================================
        // 1. EVENTOS DEL BUSCADOR Y AUTOCOMPLETADO (POPUP)
        // =================================================================

        private void TxtBuscador_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PopupSugerencias.IsOpen = false;
                BuscarYAgregarProducto();
            }
            else if (e.Key == Key.Down && PopupSugerencias.IsOpen)
            {
                LstSugerencias.Focus();
                if (LstSugerencias.Items.Count > 0)
                    LstSugerencias.SelectedIndex = 0;
            }
        }

        private void TxtBuscador_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = TxtBuscador.Text.Trim();

            if (string.IsNullOrEmpty(busqueda))
            {
                PopupSugerencias.IsOpen = false;
                return;
            }

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
            TxtBuscador.TextChanged -= TxtBuscador_TextChanged;
            TxtBuscador.Text = productoSeleccionado.CodigoBarras;
            TxtBuscador.TextChanged += TxtBuscador_TextChanged;

            BuscarYAgregarProducto();

            LstSugerencias.SelectedItem = null;
            TxtBuscador.Focus();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarYAgregarProducto();
        }

        // =================================================================
        // 2. LÓGICA PRINCIPAL: AGREGAR AL CARRITO (CON CANTIDAD)
        // =================================================================
        private void BuscarYAgregarProducto()
        {
            string busqueda = TxtBuscador.Text.Trim();
            if (string.IsNullOrEmpty(busqueda)) return;

            // Leemos y validamos la cantidad ingresada (Si escriben letras, ponemos 1 por defecto)
            if (!decimal.TryParse(TxtCantidad.Text, out decimal cantidadAAgregar) || cantidadAAgregar <= 0)
            {
                cantidadAAgregar = 1;
            }

            var producto = _context.Productos
                .FirstOrDefault(p => (p.CodigoBarras == busqueda || p.Descripcion == busqueda) && p.Activo == true);

            if (producto != null)
            {
                if (producto.StockActual <= 0 || producto.StockActual < cantidadAAgregar)
                {
                    MessageBox.Show($"No hay stock suficiente. Solo quedan {producto.StockActual} disponibles.", "Stock Insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtBuscador.SelectAll();
                    return;
                }

                var itemEnCarrito = _carrito.FirstOrDefault(c => c.ProductoID == producto.ProductoID);

                if (itemEnCarrito != null)
                {
                    if ((itemEnCarrito.Cantidad + cantidadAAgregar) > producto.StockActual)
                    {
                        MessageBox.Show($"Stock insuficiente para agregar {cantidadAAgregar} más. Solo quedan {producto.StockActual} disponibles.", "Stock Bajo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        itemEnCarrito.Cantidad += cantidadAAgregar;
                        DgCarrito.Items.Refresh();
                    }
                }
                else
                {
                    _carrito.Add(new DetalleVentaTemporal
                    {
                        ProductoID = producto.ProductoID,
                        Codigo = producto.CodigoBarras,
                        Descripcion = producto.Descripcion,
                        PrecioCosto = producto.PrecioCosto,
                        PrecioVenta = producto.PrecioVenta,
                        Cantidad = cantidadAAgregar
                    });
                }

                ActualizarTotales();

                // Limpiamos el buscador y restauramos la cantidad a "1"
                TxtCantidad.Text = "1";
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
            TxtTotalArticulos.Text = _carrito.Sum(x => x.Cantidad).ToString("N2");
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
                TxtCambio.Text = cambio < 0 ? "$ 0.00" : cambio.ToString("C2");
            }
            else
            {
                TxtCambio.Text = "$ 0.00";
            }
        }

        // =================================================================
        // 4. PROCESAR VENTA Y GENERAR TICKET
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

                    // ¡LLAMADA A LA NUEVA CLASE PARA GENERAR EL TICKET!
                    var generador = new GeneradorTicket();
                    generador.CrearEImprimirTicket(nuevaVenta, _carrito.ToList(), efectivo, cambio);

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
            TxtCantidad.Text = "1";
            TxtBuscador.Text = "";
            TxtEfectivo.Text = "";
            TxtCambio.Text = "$ 0.00";
            ActualizarTotales();

            TxtBuscador.Focus();
        }
    }
    
}