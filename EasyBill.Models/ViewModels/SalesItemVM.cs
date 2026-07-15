using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SalesItemVM
    {
        public int Id { get; set; }
        public int SalesId { get; set; }
        public int ItemMasterId { get; set; }
        public int? HsnId { get; set; }
        public string? HsnCode { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? ItemBarcodeNumber { get; set; }
        public int? PurchaseItemId { get; set; }
        public string? Batch { get; set; }
        public decimal Qty { get; set; }
        public string? AvailableQty { get; set; }
        public decimal TabletQty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal? IGst { get; set; }
        public decimal? CGst { get; set; }
        public decimal? SGst { get; set; }
        public decimal Discount { get; set; } = 0M;
        public decimal MaximumDiscount { get; set; }
        public bool AllowNegative { get; set; }
        public decimal MinimumQty { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }

        public bool IsSoldInTablets { get; set; }
        public decimal Conversion { get; set; } = 1M;
        public string ItemConversion { get; set; } = "StripWise";
        public decimal StripRate { get; set; }
        public decimal? Cess { get; set; }
        public string? CompanyName { get; set; } 
        public string? Packing { get; set; }
        public string? Mfg { get; set; }     
        public bool Narcotics { get; set; }
        public bool ScheduleH { get; set; }
        public bool ScheduleH1 { get; set; }
        public string? CategoryName { get; set; }
    }  
}
