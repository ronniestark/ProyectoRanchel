using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class Producto
    {
        [Key]
        public int ProductoID { get; set; }

        [StringLength(50)]
        public string CodigoBarras { get; set; }

        [Required]
        [StringLength(150)]
        public string Descripcion { get; set; }

        // --- RELACIÓN CON CATEGORÍA ---
        public int CategoriaID { get; set; }
        [ForeignKey("CategoriaID")]
        public virtual Categoria Categoria { get; set; }

        // --- NUEVA RELACIÓN CON UNIDAD DE MEDIDA ---
        public int UnidadMedidaID { get; set; }
        [ForeignKey("UnidadMedidaID")]
        public virtual UnidadMedida UnidadMedida { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCosto { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal MargenGanancia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal StockActual { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal StockMinimo { get; set; } = 5;

        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
    }
}