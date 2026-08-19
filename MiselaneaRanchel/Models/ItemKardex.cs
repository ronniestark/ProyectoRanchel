using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiselaneaRanchel.Models
{
    public class ItemKardex
    {
        public DateTime Fecha { get; set; }
        public string Producto { get; set; }
        public string Tipo { get; set; } // ENTRADA o SALIDA
        public decimal Cantidad { get; set; }
        public string Motivo { get; set; }
    }
}
