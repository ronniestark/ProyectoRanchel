using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiselaneaRanchel.Models
{
    public class UnidadMedida
    {
        [Key]
        public int UnidadMedidaID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(10)]
        public string Abreviatura { get; set; }

        public bool? Activo { get; set; } = true; // Agregado el ?

        public virtual ICollection<Producto> Productos { get; set; }
    }
}