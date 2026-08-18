using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiselaneaRanchel.Models
{
    public class AsientoItem
    {
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; }
        public string Referencia { get; set; }
        public decimal Ingreso_Debe { get; set; }
        public decimal Egreso_Haber { get; set; }
    }
}
