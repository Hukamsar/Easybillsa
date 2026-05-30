using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ItemImageVM
    {
        public int Id { get; set; }
        public string? ImagePath { get; set; }
        public bool IsPrimary { get; set; }
    }
}
