using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiselaneaRanchel.Models
{
    public class CorteDeCaja
    {
        [Key]
        public int CorteCajaID { get; set; }

        public DateTime FechaCorte { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FondoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasEfectivo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalidasExtra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEsperado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EfectivoReal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Diferencia { get; set; }

        [Required]
        [StringLength(50)]
        public string EstadoCaja { get; set; }
    }
}