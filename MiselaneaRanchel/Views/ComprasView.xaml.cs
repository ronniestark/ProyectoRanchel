using System;
using System.Collections.Generic;
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
    public partial class ComprasView : UserControl
    {
        private readonly ApplicationDbContext _context;
        private ObservableCollection<DetalleCompraTemporal> _carritoCompras;
        private Producto _productoEncontrado = null;
        private List<Producto> _todosLosProductos;

        private bool _compraProcesada = false;

        public ComprasView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _carritoCompras = new ObservableCollection<DetalleCompraTemporal>();
            DgListaCompras.ItemsSource = _carritoCompras;

            CargarDatosIniciales();
            CargarHistorialCompras(); // Carga la tabla de historial al abrir
        }

        private void CargarDatosIniciales()
        {
            try
            {
                var proveedores = _context.Proveedores.Where(p => p.Activo == true).ToList();
                CmbProveedor.ItemsSource = proveedores;
                CmbProveedor.DisplayMemberPath = "Nombre";
                CmbProveedor.SelectedValuePath = "ProveedorID";

                DpFecha.SelectedDate = DateTime.Now;

                _todosLosProductos = _context.Productos.Where(p => p.Activo == true).ToList();
                CmbBuscarProducto.ItemsSource = _todosLosProductos.Select(p => new { p.ProductoID, Display = $"{p.CodigoBarras} - {p.Descripcion}" }).ToList();
                CmbBuscarProducto.DisplayMemberPath = "Display";
                CmbBuscarProducto.SelectedValuePath = "ProductoID";

                CmbBuscarProducto.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión: {ex.Message}");
            }
        }

        // =========================================================
        // PESTAÑA 2: HISTORIAL DE COMPRAS (SELECT A LA TABLA)
        // =========================================================
        private void CargarHistorialCompras()
        {
            try
            {
                // Hacemos el SELECT a la tabla Compras, incluyendo el nombre del proveedor
                var historial = _context.Compras
                                        .Include(c => c.Proveedor)
                                        .OrderByDescending(c => c.FechaCompra)
                                        .ToList();

                DgHistorialCompras.ItemsSource = historial;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar historial: {ex.Message}");
            }
        }

        private void BtnActualizarHistorial_Click(object sender, RoutedEventArgs e)
        {
            CargarHistorialCompras();
        }

        // =========================================================
        // LÓGICA DE LA PESTAÑA 1 (NUEVA COMPRA)
        // =========================================================

        private void CmbBuscarProducto_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CmbBuscarProducto.IsDropDownOpen == false)
                CmbBuscarProducto.IsDropDownOpen = true;

            var textBox = (TextBox)e.OriginalSource;
            string textoBusqueda = textBox.Text.ToLower();

            var filtrados = _todosLosProductos.Where(p =>
                (p.Descripcion != null && p.Descripcion.ToLower().Contains(textoBusqueda)) ||
                (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(textoBusqueda)))
                .Select(p => new { p.ProductoID, Display = $"{p.CodigoBarras} - {p.Descripcion}" })
                .ToList();

            CmbBuscarProducto.ItemsSource = filtrados;
        }

        private void CmbBuscarProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbBuscarProducto.SelectedValue != null)
            {
                int idSeleccionado = (int)CmbBuscarProducto.SelectedValue;
                _productoEncontrado = _todosLosProductos.FirstOrDefault(p => p.ProductoID == idSeleccionado);

                if (_productoEncontrado != null)
                {
                    TxtCosto.Text = _productoEncontrado.PrecioCosto.ToString("0.00");
                    TxtCantidad.Text = "1";
                    TxtCantidad.Focus();
                    TxtCantidad.SelectAll();
                }
            }
        }

        private void CmbBuscarProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _productoEncontrado == null)
            {
                string codigo = CmbBuscarProducto.Text;
                _productoEncontrado = _todosLosProductos.FirstOrDefault(p => p.CodigoBarras == codigo);

                if (_productoEncontrado != null)
                {
                    TxtCosto.Text = _productoEncontrado.PrecioCosto.ToString("0.00");
                    TxtCantidad.Text = "1";
                    TxtCantidad.Focus();
                    TxtCantidad.SelectAll();
                }
            }
        }

        private void TxtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnAgregarProducto_Click(null, null);
            }
        }

        private void BtnAgregarProducto_Click(object sender, RoutedEventArgs e)
        {
            if (_productoEncontrado == null)
            {
                MessageBox.Show("Selecciona un producto del buscador.");
                return;
            }

            if (!decimal.TryParse(TxtCosto.Text, out decimal costoUnitario) || costoUnitario < 0)
            {
                MessageBox.Show("Costo unitario no válido.");
                return;
            }

            if (!decimal.TryParse(TxtCantidad.Text, out decimal cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Cantidad no válida.");
                return;
            }

            var itemExistente = _carritoCompras.FirstOrDefault(c => c.ProductoID == _productoEncontrado.ProductoID);
            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
                itemExistente.CostoUnitario = costoUnitario;
                DgListaCompras.Items.Refresh();
            }
            else
            {
                _carritoCompras.Add(new DetalleCompraTemporal
                {
                    ProductoID = _productoEncontrado.ProductoID,
                    Codigo = _productoEncontrado.CodigoBarras,
                    Descripcion = _productoEncontrado.Descripcion,
                    Cantidad = cantidad,
                    CostoUnitario = costoUnitario
                });
            }

            ActualizarTotales();
            LimpiarBuscador();
        }

        private void ActualizarTotales()
        {
            decimal totalFactura = _carritoCompras.Sum(item => item.SubTotal);
            TxtTotalFactura.Text = $"$ {totalFactura.ToString("N2")}";
        }

        private void LimpiarBuscador()
        {
            _productoEncontrado = null;
            CmbBuscarProducto.Text = "";
            CmbBuscarProducto.SelectedIndex = -1;
            TxtCosto.Text = "";
            TxtCantidad.Text = "";

            CmbBuscarProducto.ItemsSource = _todosLosProductos.Select(p => new { p.ProductoID, Display = $"{p.CodigoBarras} - {p.Descripcion}" }).ToList();
            CmbBuscarProducto.Focus();
        }

        // =========================================================
        // PROCESAR COMPRA
        // =========================================================
        private void BtnProcesarCompra_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProveedor.SelectedValue == null)
            {
                MessageBox.Show("Selecciona un proveedor.");
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtFactura.Text))
            {
                MessageBox.Show("Ingresa la factura o remisión.");
                return;
            }
            if (_carritoCompras.Count == 0)
            {
                MessageBox.Show("El carrito está vacío.");
                return;
            }

            using (var transaccion = _context.Database.BeginTransaction())
            {
                try
                {
                    var nuevaCompra = new Compra
                    {
                        ProveedorID = (int)CmbProveedor.SelectedValue,
                        NumeroFactura = TxtFactura.Text,
                        FechaCompra = DpFecha.SelectedDate ?? DateTime.Now,
                        TotalCompra = _carritoCompras.Sum(c => c.SubTotal),
                        Estado = "COMPLETADO"
                    };

                    _context.Compras.Add(nuevaCompra);
                    _context.SaveChanges();

                    foreach (var item in _carritoCompras)
                    {
                        var detalle = new DetalleCompra
                        {
                            CompraID = nuevaCompra.CompraID,
                            ProductoID = item.ProductoID,
                            Cantidad = item.Cantidad,
                            CostoUnitario = item.CostoUnitario,
                            SubTotal = item.SubTotal
                        };
                        _context.DetalleCompras.Add(detalle);

                        var productoBase = _context.Productos.Find(item.ProductoID);
                        if (productoBase != null)
                        {
                            productoBase.StockActual += item.Cantidad;
                            productoBase.PrecioCosto = item.CostoUnitario;
                            productoBase.PrecioVenta = productoBase.PrecioCosto + (productoBase.PrecioCosto * (productoBase.MargenGanancia / 100m));
                            _context.Productos.Update(productoBase);
                        }

                        var movimiento = new MovimientoInventario
                        {
                            ProductoID = item.ProductoID,
                            TipoMovimiento = "ENTRADA",
                            Cantidad = item.Cantidad,
                            FechaMovimiento = DateTime.Now,
                            Motivo = $"Compra a Proveedor (Fac: {nuevaCompra.NumeroFactura})",
                            ReferenciaID = nuevaCompra.CompraID
                        };
                        _context.MovimientosInventario.Add(movimiento);
                    }

                    _context.SaveChanges();
                    transaccion.Commit();

                    MessageBox.Show("¡Factura guardada exitosamente en la base de datos!\nLos datos permanecerán en pantalla para tu revisión.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Actualizar memoria y la tabla de historial
                    _todosLosProductos = _context.Productos.Where(p => p.Activo == true).ToList();
                    CargarHistorialCompras();

                    // Bloqueamos los controles en lugar de borrar la pantalla
                    _compraProcesada = true;
                    BloquearPantalla(true);
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void BtnLimpiarNueva_Click(object sender, RoutedEventArgs e)
        {
            if (_carritoCompras.Count > 0 && !_compraProcesada)
            {
                if (MessageBox.Show("¿Seguro que deseas descartar esta factura no guardada?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    ReiniciarPantalla();
                }
            }
            else
            {
                ReiniciarPantalla();
            }
        }

        private void ReiniciarPantalla()
        {
            CmbProveedor.SelectedIndex = -1;
            TxtFactura.Text = "";
            DpFecha.SelectedDate = DateTime.Now;

            LimpiarBuscador();
            _carritoCompras.Clear();
            ActualizarTotales();

            _compraProcesada = false;
            BloquearPantalla(false);
        }

        private void BloquearPantalla(bool bloquear)
        {
            BtnProcesar.IsEnabled = !bloquear;
            BtnAgregar.IsEnabled = !bloquear;
            CmbBuscarProducto.IsEnabled = !bloquear;
            CmbProveedor.IsEnabled = !bloquear;
            TxtFactura.IsReadOnly = bloquear;
        }
    }

    public class DetalleCompraTemporal
    {
        public int ProductoID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal SubTotal => Cantidad * CostoUnitario;
    }
}