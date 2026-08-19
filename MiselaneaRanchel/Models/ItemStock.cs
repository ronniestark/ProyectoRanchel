using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiselaneaRanchel.Models
{
    public class ItemStock
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
        public decimal StockActual { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal ValorTotal => StockActual * CostoUnitario;
    }
}
