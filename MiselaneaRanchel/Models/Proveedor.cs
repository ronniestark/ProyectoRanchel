using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiselaneaRanchel.Models
{
    public class Proveedor
    {
        [Key]
        public int ProveedorID { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; } // Agregado el ?

        public bool? Activo { get; set; } = true; // Agregado el ?
        public DateTime? FechaRegistro { get; set; } = DateTime.Now; // Agregado el ?

        public virtual ICollection<Compra> Compras { get; set; }
    }
}