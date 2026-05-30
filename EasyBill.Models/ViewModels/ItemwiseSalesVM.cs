using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ItemwiseSalesVM
    {
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string CompanyName { get; set; }
        public string DivisionName { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal MRP { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

}
