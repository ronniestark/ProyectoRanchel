using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiselaneaRanchel.Models
{
    public class Categoria
    {
        [Key]
        public int CategoriaID { get; set; }

        [Required]
        [StringLength(50)]
        public string Nombre { get; set; }

        public bool? Activo { get; set; } = true; // Agregado el ?
        public DateTime? FechaRegistro { get; set; } = DateTime.Now; // Agregado el ?

        public virtual ICollection<Producto> Productos { get; set; }
    }
}