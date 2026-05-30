using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class StockReturnItemVM 
    {
        public int Id { get; set; }
        public int StockReturnId { get; set; }
        public int ItemMasterId { get; set; }
        public string? ItemName { get; set; }
        public int? PurchaseItemId { get; set; }
        public string? Batch { get; set; }
        public decimal Mrp { get; set; }
        public decimal Qty { get; set; }
        public decimal TabletQty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal Discount { get; set; } = 0M;
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public string? Reason { get; set; }
        public decimal Cess { get; set; }
        public decimal StripRate { get; set; }
    }
}
