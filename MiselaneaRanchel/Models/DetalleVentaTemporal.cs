using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiselaneaRanchel.Models
{
    public class DetalleVentaTemporal
    {
        public int ProductoID { get; set; }
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal PrecioCosto { get; set; }

        private decimal _cantidad;
        public decimal Cantidad
        {
            get => _cantidad;
            set { _cantidad = value; }
        }

        public decimal SubTotal => Cantidad * PrecioVenta;
    }
}
