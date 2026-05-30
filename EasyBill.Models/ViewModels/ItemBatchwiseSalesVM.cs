using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class ItemBatchwiseSalesVM
    {
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }

        public int ItemMasterId { get; set; }
        public string ItemName { get; set; }

        public string BatchNo { get; set; }
        public string? StockDisplay { get; set; }
        public decimal Qty { get; set; }
        public decimal Mrp { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

}
