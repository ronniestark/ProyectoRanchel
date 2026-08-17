using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Views
{
    public partial class CatalogosView : UserControl
    {
        private readonly ApplicationDbContext _context;

        private Categoria _categoriaSeleccionada = null;
        private Proveedor _proveedorSeleccionado = null;
        private UnidadMedida _unidadSeleccionada = null;

        public CatalogosView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            CargarTodasLasTablas();
        }

        private void CargarTodasLasTablas()
        {
            CargarCategorias();
            CargarProveedores();
            CargarUnidades();
        }

        // =========================================================
        // 1. CRUD CATEGORÍAS
        // =========================================================
        private void CargarCategorias()
        {
            // Validamos que Activo sea igual a true
            DgCategorias.ItemsSource = _context.Categorias.Where(c => c.Activo == true).ToList();
        }

        private void BtnCatGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCatNombre.Text)) return;

            if (_categoriaSeleccionada == null)
            {
                _context.Categorias.Add(new Categoria { Nombre = TxtCatNombre.Text, Activo = true, FechaRegistro = DateTime.Now });
            }
            else
            {
                _categoriaSeleccionada.Nombre = TxtCatNombre.Text;
                _context.Categorias.Update(_categoriaSeleccionada);
            }
            _context.SaveChanges();
            LimpiarFormularioCategoria();
            CargarCategorias();
        }

        private void BtnCatEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_categoriaSeleccionada != null)
            {
                if (MessageBox.Show("¿Eliminar categoría?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _categoriaSeleccionada.Activo = false;
                    _context.Categorias.Update(_categoriaSeleccionada);
                    _context.SaveChanges();
                    LimpiarFormularioCategoria();
                    CargarCategorias();
                }
            }
        }

        private void DgCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgCategorias.SelectedItem is Categoria cat)
            {
                _categoriaSeleccionada = cat;
                // Prevenimos nulos en el Textbox usando ?? ""
                TxtCatNombre.Text = cat.Nombre ?? "";
                BtnCatGuardar.Content = "Actualizar Categoría";
                BtnCatEliminar.Visibility = Visibility.Visible;
            }
        }

        private void BtnCatLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormularioCategoria();

        private void LimpiarFormularioCategoria()
        {
            _categoriaSeleccionada = null;
            TxtCatNombre.Text = "";
            BtnCatGuardar.Content = "Guardar Categoría";
            BtnCatEliminar.Visibility = Visibility.Collapsed;
            DgCategorias.SelectedItem = null;
        }

        // =========================================================
        // 2. CRUD PROVEEDORES
        // =========================================================
        private void CargarProveedores()
        {
            // Validamos que Activo sea igual a true
            DgProveedores.ItemsSource = _context.Proveedores.Where(p => p.Activo == true).ToList();
        }

        private void BtnProvGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtProvNombre.Text)) return;

            if (_proveedorSeleccionado == null)
            {
                _context.Proveedores.Add(new Proveedor
                {
                    Nombre = TxtProvNombre.Text,
                    Telefono = TxtProvTelefono.Text,
                    Activo = true,
                    FechaRegistro = DateTime.Now
                });
            }
            else
            {
                _proveedorSeleccionado.Nombre = TxtProvNombre.Text;
                _proveedorSeleccionado.Telefono = TxtProvTelefono.Text;
                _context.Proveedores.Update(_proveedorSeleccionado);
            }
            _context.SaveChanges();
            LimpiarFormularioProveedor();
            CargarProveedores();
        }

        private void BtnProvEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_proveedorSeleccionado != null)
            {
                if (MessageBox.Show("¿Eliminar proveedor?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _proveedorSeleccionado.Activo = false;
                    _context.Proveedores.Update(_proveedorSeleccionado);
                    _context.SaveChanges();
                    LimpiarFormularioProveedor();
                    CargarProveedores();
                }
            }
        }

        private void DgProveedores_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgProveedores.SelectedItem is Proveedor prov)
            {
                _proveedorSeleccionado = prov;
                TxtProvNombre.Text = prov.Nombre ?? "";

                // Si el teléfono viene NULL de la base de datos, le asignamos texto vacío
                TxtProvTelefono.Text = prov.Telefono ?? "";

                BtnProvGuardar.Content = "Actualizar Proveedor";
                BtnProvEliminar.Visibility = Visibility.Visible;
            }
        }

        private void BtnProvLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormularioProveedor();

        private void LimpiarFormularioProveedor()
        {
            _proveedorSeleccionado = null;
            TxtProvNombre.Text = "";
            TxtProvTelefono.Text = "";
            BtnProvGuardar.Content = "Guardar Proveedor";
            BtnProvEliminar.Visibility = Visibility.Collapsed;
            DgProveedores.SelectedItem = null;
        }

        // =========================================================
        // 3. CRUD UNIDADES DE MEDIDA
        // =========================================================
        private void CargarUnidades()
        {
            // Validamos que Activo sea igual a true
            DgUnidades.ItemsSource = _context.UnidadesMedida.Where(u => u.Activo == true).ToList();
        }

        private void BtnUniGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUniNombre.Text) || string.IsNullOrWhiteSpace(TxtUniAbreviatura.Text)) return;

            if (_unidadSeleccionada == null)
            {
                _context.UnidadesMedida.Add(new UnidadMedida
                {
                    Nombre = TxtUniNombre.Text,
                    Abreviatura = TxtUniAbreviatura.Text,
                    Activo = true
                });
            }
            else
            {
                _unidadSeleccionada.Nombre = TxtUniNombre.Text;
                _unidadSeleccionada.Abreviatura = TxtUniAbreviatura.Text;
                _context.UnidadesMedida.Update(_unidadSeleccionada);
            }
            _context.SaveChanges();
            LimpiarFormularioUnidad();
            CargarUnidades();
        }

        private void BtnUniEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_unidadSeleccionada != null)
            {
                if (MessageBox.Show("¿Eliminar unidad de medida?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _unidadSeleccionada.Activo = false;
                    _context.UnidadesMedida.Update(_unidadSeleccionada);
                    _context.SaveChanges();
                    LimpiarFormularioUnidad();
                    CargarUnidades();
                }
            }
        }

        private void DgUnidades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgUnidades.SelectedItem is UnidadMedida uni)
            {
                _unidadSeleccionada = uni;
                TxtUniNombre.Text = uni.Nombre ?? "";
                TxtUniAbreviatura.Text = uni.Abreviatura ?? "";
                BtnUniGuardar.Content = "Actualizar Unidad";
                BtnUniEliminar.Visibility = Visibility.Visible;
            }
        }

        private void BtnUniLimpiar_Click(object sender, RoutedEventArgs e) => LimpiarFormularioUnidad();

        private void LimpiarFormularioUnidad()
        {
            _unidadSeleccionada = null;
            TxtUniNombre.Text = "";
            TxtUniAbreviatura.Text = "";
            BtnUniGuardar.Content = "Guardar Unidad";
            BtnUniEliminar.Visibility = Visibility.Collapsed;
            DgUnidades.SelectedItem = null;
        }
    }
}