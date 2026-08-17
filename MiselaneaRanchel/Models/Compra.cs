using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class Compra
    {
        [Key]
        public int CompraID { get; set; }

        public int ProveedorID { get; set; }
        [ForeignKey("ProveedorID")]
        public virtual Proveedor Proveedor { get; set; }

        [StringLength(50)]
        public string NumeroFactura { get; set; }

        public DateTime FechaCompra { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCompra { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "COMPLETADO";

        public virtual ICollection<DetalleCompra> Detalles { get; set; }
    }
}