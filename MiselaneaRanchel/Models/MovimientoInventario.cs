using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class MovimientoInventario
    {
        [Key]
        public int MovimientoID { get; set; }

        public int ProductoID { get; set; }
        [ForeignKey("ProductoID")]
        public virtual Producto Producto { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoMovimiento { get; set; } // 'ENTRADA', 'SALIDA', 'MERMA'

        [Column(TypeName = "decimal(18,3)")]
        public decimal Cantidad { get; set; }

        public DateTime FechaMovimiento { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string Motivo { get; set; }

        public int? ReferenciaID { get; set; } // Opcional (Nullable)
    }
}