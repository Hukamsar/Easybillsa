using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class HoldSalesListVM
    {
        public int Id { get; set; }
        public string HoldToken { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public int ItemCount { get; set; }
        public DateTime HoldDate { get; set; }
    }
}
