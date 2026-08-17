using Microsoft.EntityFrameworkCore;
using MiselaneaRanchel.Models;

namespace MiselaneaRanchel.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<DetalleCompra> DetalleCompras { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
        public DbSet<CorteDeCaja> CortesDeCaja { get; set; }
        public DbSet<UnidadMedida> UnidadesMedida { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Aquí configuras tu cadena de conexión a SQL Server
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=MiscelaneaRanchelDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }
    }
}