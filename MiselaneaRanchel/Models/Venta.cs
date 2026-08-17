using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class Venta
    {
        [Key]
        public int VentaID { get; set; }

        [Required]
        [StringLength(20)]
        public string NumeroTicket { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EfectivoRecibido { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CambioEntregado { get; set; }

        [StringLength(20)]
        public string Estado { get; set; } = "COMPLETADO";

        // Relación: Una venta tiene muchos detalles
        public virtual ICollection<DetalleVenta> Detalles { get; set; }
    }
}