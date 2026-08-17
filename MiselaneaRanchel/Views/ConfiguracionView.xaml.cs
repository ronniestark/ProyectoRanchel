using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class ConfiguracionView : UserControl
    {
        private readonly ApplicationDbContext _context;
        private Producto _productoSeleccionado = null;

        public ConfiguracionView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            CargarCombos();
            CargarProductos();
        }

        // =================================================================
        // 1. CARGA DE DATOS (READ)
        // =================================================================
        private void CargarCombos()
        {
            // Llenar ComboBox de Categorías
            var categorias = _context.Categorias.Where(c => c.Activo == true).ToList();
            CmbCategoria.ItemsSource = categorias;
            CmbCategoria.DisplayMemberPath = "Nombre";
            CmbCategoria.SelectedValuePath = "CategoriaID";

            // Llenar ComboBox de Unidades de Medida
            var unidades = _context.UnidadesMedida.Where(u => u.Activo == true).ToList();
            CmbUnidad.ItemsSource = unidades;
            CmbUnidad.DisplayMemberPath = "Nombre";
            CmbUnidad.SelectedValuePath = "UnidadMedidaID";
        }

        private void CargarProductos(string filtro = "")
        {
            // Usamos Include para traer el Nombre de la Categoría y Unidad en la misma consulta
            var query = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.UnidadMedida)
                .Where(p => p.Activo == true);

            // Si hay texto en el buscador, filtramos la consulta
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(p => p.Descripcion.Contains(filtro) || p.CodigoBarras.Contains(filtro));
            }

            DgProductos.ItemsSource = query.ToList();
        }

        // Evento para el buscador en tiempo real
        private void TxtBuscarGrilla_TextChanged(object sender, TextChangedEventArgs e)
        {
            CargarProductos(TxtBuscarGrilla.Text);
        }

        // =================================================================
        // 2. LÓGICA DE CÁLCULO DE PRECIO AUTOMÁTICO
        // =================================================================
        private void CalcularPrecioFinal_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Este evento se dispara cada vez que escribes en Costo o Ganancia
            if (decimal.TryParse(TxtCosto.Text, out decimal costo) &&
                decimal.TryParse(TxtGanancia.Text, out decimal ganancia))
            {
                // Fórmula: PrecioVenta = Costo + (Costo * (Ganancia / 100))
                decimal precioFinal = costo + (costo * (ganancia / 100m));
                TxtPrecioVenta.Text = precioFinal.ToString("F2");
            }
            else
            {
                TxtPrecioVenta.Text = "0.00";
            }
        }

        // =================================================================
        // 3. GUARDAR / ACTUALIZAR (CREATE / UPDATE)
        // =================================================================
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("El nombre del producto es obligatorio.");
                return;
            }
            if (CmbCategoria.SelectedValue == null)
            {
                MessageBox.Show("Debes seleccionar una categoría.");
                return;
            }
            if (CmbUnidad.SelectedValue == null)
            {
                MessageBox.Show("Debes seleccionar una unidad de medida.");
                return;
            }
            if (!decimal.TryParse(TxtCosto.Text, out decimal costo))
            {
                MessageBox.Show("El costo debe ser un número válido.");
                return;
            }
            if (!decimal.TryParse(TxtGanancia.Text, out decimal ganancia))
            {
                MessageBox.Show("El margen de ganancia debe ser un número válido.");
                return;
            }
            if (!decimal.TryParse(TxtPrecioVenta.Text, out decimal precioVenta))
            {
                precioVenta = 0;
            }

            // Mapeo
            if (_productoSeleccionado == null)
            {
                // INSERTAR
                var nuevoProducto = new Producto
                {
                    CodigoBarras = TxtCodigo.Text,
                    Descripcion = TxtNombre.Text,
                    CategoriaID = (int)CmbCategoria.SelectedValue,
                    UnidadMedidaID = (int)CmbUnidad.SelectedValue,
                    PrecioCosto = costo,
                    MargenGanancia = ganancia,
                    PrecioVenta = precioVenta,
                    StockActual = 0, // Inicia en 0, se carga por Compras
                    Activo = true,
                    FechaRegistro = DateTime.Now
                };
                _context.Productos.Add(nuevoProducto);
                MessageBox.Show("Producto guardado correctamente.");
            }
            else
            {
                // EDITAR
                _productoSeleccionado.CodigoBarras = TxtCodigo.Text;
                _productoSeleccionado.Descripcion = TxtNombre.Text;
                _productoSeleccionado.CategoriaID = (int)CmbCategoria.SelectedValue;
                _productoSeleccionado.UnidadMedidaID = (int)CmbUnidad.SelectedValue;
                _productoSeleccionado.PrecioCosto = costo;
                _productoSeleccionado.MargenGanancia = ganancia;
                _productoSeleccionado.PrecioVenta = precioVenta;

                _context.Productos.Update(_productoSeleccionado);
                MessageBox.Show("Producto actualizado correctamente.");
            }

            _context.SaveChanges();
            LimpiarFormulario();
            CargarProductos();
        }

        // =================================================================
        // 4. DAR DE BAJA LÓGICA (DELETE)
        // =================================================================
        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_productoSeleccionado != null)
            {
                if (MessageBox.Show($"¿Estás seguro de dar de baja: {_productoSeleccionado.Descripcion}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _productoSeleccionado.Activo = false; // Borrado lógico
                    _context.Productos.Update(_productoSeleccionado);
                    _context.SaveChanges();

                    LimpiarFormulario();
                    CargarProductos();
                }
            }
        }

        // =================================================================
        // 5. SELECCIÓN PARA EDITAR Y LIMPIEZA
        // =================================================================
        private void DgProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgProductos.SelectedItem is Producto prod)
            {
                _productoSeleccionado = prod;

                // Mapear datos a la UI (usando ?? "" para evitar nulos)
                TxtCodigo.Text = prod.CodigoBarras ?? "";
                TxtNombre.Text = prod.Descripcion ?? "";
                CmbCategoria.SelectedValue = prod.CategoriaID;
                CmbUnidad.SelectedValue = prod.UnidadMedidaID;
                TxtCosto.Text = prod.PrecioCosto.ToString("0.00");
                TxtGanancia.Text = prod.MargenGanancia.ToString("0.00");

                // Forzar el recálculo visual llamando al método
                CalcularPrecioFinal_TextChanged(null, null);

                BtnGuardar.Content = "Actualizar Producto";
                BtnEliminar.Visibility = Visibility.Visible;
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            _productoSeleccionado = null;
            TxtCodigo.Text = "";
            TxtNombre.Text = "";
            CmbCategoria.SelectedIndex = -1;
            CmbUnidad.SelectedIndex = -1;
            TxtCosto.Text = "";
            TxtGanancia.Text = "";
            TxtPrecioVenta.Text = "";

            BtnGuardar.Content = "Guardar Producto";
            BtnEliminar.Visibility = Visibility.Collapsed;
            DgProductos.SelectedItem = null;
            TxtBuscarGrilla.Text = "";
        }
    }
}