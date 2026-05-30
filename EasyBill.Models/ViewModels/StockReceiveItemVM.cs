using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class StockReceiveItemVM
    {
        public int Id { get; set; }
        public int StockReceiveId { get; set; }
        public int ItemMasterId { get; set; }
        public int? PurchaseItemId { get; set; }
        public int? SourcePurchaseChallanId { get; set; }
        public string? ItemName { get; set; }
        public string? Batch { get; set; }
        public int FreeQty { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal SalesRateA { get; set; }
        public decimal? SalesRateB { get; set; }
        public int? HsnId { get; set; }
        public decimal Gst { get; set; }
        public decimal GstAmount { get; set; }
        public decimal CGst { get; set; }
        public decimal SGst { get; set; }
        public decimal CGstAmount { get; set; }
        public decimal SGstAmount { get; set; }
        public decimal SCGst { get; set; }
        public decimal Discount { get; set; } = 0M;
        public decimal DiscountAmt { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }
        public decimal Cess { get; set; }
        public decimal BatchWiseCose { get; set; }
        public string? Barcode { get; set; }
    }
}
