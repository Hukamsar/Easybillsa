using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Model.DTO
{
    public class ItemPurchaseHistoryDto
    {
        public string Supplier { get; set; }
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal Qty { get; set; }
        public decimal FreeQty { get; set; }
        public string Batch { get; set; }
        public decimal Mrp { get; set; }
        public decimal Rate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal BatchWiseCost { get; set; }
    }
}
