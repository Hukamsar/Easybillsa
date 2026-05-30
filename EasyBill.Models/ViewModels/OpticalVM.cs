using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class OpticalVM
    {
        public int Id { get; set; }
        public decimal Sphere { get; set; }
        public decimal Cylinder { get; set; }
        public decimal Axis { get; set; }
        public decimal Prism { get; set; }
        public decimal Add { get; set; }
        public string Rx { get; set; }
        public int ItemMasterId { get; set; }
    }
}
