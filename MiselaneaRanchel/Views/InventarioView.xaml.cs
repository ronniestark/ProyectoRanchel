using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Data;
using MiselaneaRanchel.Models;
// Necesario para usar InputBox. Si te da error, asegúrate de tener la referencia a Microsoft.VisualBasic
using Microsoft.VisualBasic;

namespace MiselaneaRanchel.Views
{
    public partial class InventarioView : UserControl
    {
        private readonly ApplicationDbContext _context;
        private List<Producto> _todosLosProductos;

        public InventarioView()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();

            CargarCategoriasFiltro();
            CargarInventario();
        }

        private void CargarCategoriasFiltro()
        {
            try
            {
                var categorias = _context.Categorias.Where(c => c.Activo == true).ToList();

                // Agregamos una opción "Todas" al principio
                categorias.Insert(0, new Categoria { CategoriaID = 0, Nombre = "Todas las categorías" });

                CmbCategoriaFiltro.ItemsSource = categorias;
                CmbCategoriaFiltro.DisplayMemberPath = "Nombre";
                CmbCategoriaFiltro.SelectedValuePath = "CategoriaID";
                CmbCategoriaFiltro.SelectedIndex = 0; // Seleccionar "Todas" por defecto
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar categorías: {ex.Message}");
            }
        }

        private void CargarInventario()
        {
            try
            {
                // Cargamos todos los productos activos con sus relaciones
                _todosLosProductos = _context.Productos
                    .Include(p => p.Categoria)
                    .Include(p => p.UnidadMedida)
                    .Where(p => p.Activo == true)
                    .ToList();

                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el inventario: {ex.Message}");
            }
        }

        // Este evento se llama cada vez que cambia el texto, la categoría o el checkbox
        private void Filtros_Changed(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        // =========================================================
        // LÓGICA DE FILTRADO
        // =========================================================
        private void AplicarFiltros()
        {
            if (_todosLosProductos == null) return;

            var listaFiltrada = _todosLosProductos.AsEnumerable();

            // 1. Ocultar el Hint del buscador si hay texto
            string textoBusqueda = TxtBuscador.Text.Trim().ToLower();
            TxtHintBuscador.Visibility = string.IsNullOrEmpty(textoBusqueda) ? Visibility.Visible : Visibility.Hidden;

            // 2. Filtro de Texto (Código o Nombre)
            if (!string.IsNullOrEmpty(textoBusqueda))
            {
                listaFiltrada = listaFiltrada.Where(p =>
                    (p.Descripcion != null && p.Descripcion.ToLower().Contains(textoBusqueda)) ||
                    (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(textoBusqueda)));
            }

            // 3. Filtro por Categoría
            if (CmbCategoriaFiltro.SelectedValue != null)
            {
                int categoriaId = (int)CmbCategoriaFiltro.SelectedValue;
                if (categoriaId > 0) // Si no es "Todas"
                {
                    listaFiltrada = listaFiltrada.Where(p => p.CategoriaID == categoriaId);
                }
            }

            // 4. Filtro Solo Stock Bajo
            if (ChkStockBajo.IsChecked == true)
            {
                listaFiltrada = listaFiltrada.Where(p => p.StockActual <= p.StockMinimo);
            }

            var resultadoFinal = listaFiltrada.ToList();

            // 5. Asignar a la grilla y actualizar Tarjetas
            DgInventario.ItemsSource = resultadoFinal;
            ActualizarTarjetasResumen();
        }

        private void ActualizarTarjetasResumen()
        {
            if (_todosLosProductos != null)
            {
                TxtTotalReferencias.Text = _todosLosProductos.Count.ToString("N0");
                TxtStockBajo.Text = _todosLosProductos.Count(p => p.StockActual <= p.StockMinimo).ToString("N0");
            }
        }

        // =========================================================
        // REGISTRO DE MERMA / AJUSTE DE INVENTARIO
        // =========================================================
        private void BtnRegistrarMerma_Click(object sender, RoutedEventArgs e)
        {
            if (DgInventario.SelectedItem is Producto productoSeleccionado)
            {
                // Usamos InputBox de VisualBasic para pedir la cantidad rápido
                string cantidadStr = Interaction.InputBox(
                    $"Producto: {productoSeleccionado.Descripcion}\nStock Actual: {productoSeleccionado.StockActual}\n\nIngresa la cantidad a DESCONTAR por concepto de Merma/Ajuste:",
                    "Registrar Merma", "0");

                if (string.IsNullOrWhiteSpace(cantidadStr) || cantidadStr == "0") return;

                if (decimal.TryParse(cantidadStr, out decimal cantidadMerma) && cantidadMerma > 0)
                {
                    if (cantidadMerma > productoSeleccionado.StockActual)
                    {
                        MessageBox.Show("No puedes descontar más cantidad de la que hay en el inventario actual.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string motivo = Interaction.InputBox(
                        "Escribe el motivo de la merma (Ej. Dañado, Caducado, Uso interno):",
                        "Motivo", "Dañado");

                    if (string.IsNullOrWhiteSpace(motivo)) return; // Si cancela

                    // Guardar en la Base de Datos
                    try
                    {
                        // 1. Buscar el producto real en el contexto para actualizarlo
                        var productoBd = _context.Productos.Find(productoSeleccionado.ProductoID);
                        if (productoBd != null)
                        {
                            productoBd.StockActual -= cantidadMerma; // Restamos el stock
                            _context.Productos.Update(productoBd);

                            // 2. Registrar el movimiento en la tabla de auditoría
                            var movimiento = new MovimientoInventario
                            {
                                ProductoID = productoBd.ProductoID,
                                TipoMovimiento = "MERMA",
                                Cantidad = cantidadMerma,
                                FechaMovimiento = DateTime.Now,
                                Motivo = motivo
                            };
                            _context.MovimientosInventario.Add(movimiento);

                            _context.SaveChanges();

                            MessageBox.Show($"Merma registrada. Nuevo stock: {productoBd.StockActual}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Recargar todo para actualizar visualmente
                            CargarInventario();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar la merma: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Ingresa una cantidad numérica válida.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Primero selecciona un producto de la tabla haciendo clic en él.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}