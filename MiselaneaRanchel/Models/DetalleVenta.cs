using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class DetalleVenta
    {
        [Key]
        public int DetalleVentaID { get; set; }

        public int VentaID { get; set; }
        [ForeignKey("VentaID")]
        public virtual Venta Venta { get; set; }

        public int ProductoID { get; set; }
        [ForeignKey("ProductoID")]
        public virtual Producto Producto { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCostoHistorico { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVentaHistorico { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
    }
}