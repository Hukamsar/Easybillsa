using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseItemVM
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; } 
        public string? Purchases { get; set; } 
        public int ItemId { get; set; } 
        public string? ItemName { get; set; }
        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal FreeQty { get; set; } // MERGED FROM TL
        public decimal Qty { get; set; } // MERGED FROM TL
        public string? Unit { get; set; }
        public decimal Rate { get; set; }
        public int? HsnId { get; set; }
        public decimal Gst { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmt { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmt { get; set; }
        public decimal BatchWiseCose { get; set; }
        public decimal Mrp { get; set; }
        public decimal salserateA { get; set; }
        public decimal salserateB { get; set; }
        public string? Barcode { get; set; }
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string? SupplierName { get; set; }
        public string? CompanyName { get; set; }
        public decimal PurchaseAmount { get; set; }
        public List<string>? ExampleData { get; set; }
        public decimal BillValue { get; set; }
        public string? Packing { get; set; }
        public decimal AvgRate { get; set; }
        public decimal AvgAmount { get; set; }
        public decimal AvgSales { get; set; }
        public int MaximumQty { get; set; }
        public int ExtraQty { get; set; }
        public int? AvailableQty { get; set; }
        public decimal CGst { get; set; }
        public decimal SGst { get; set; }
        public decimal CGstAmount { get; set; }
        public decimal SGstAmount { get; set; }
        public decimal SCGst { get; set; }
        public decimal CurrentQty { get; set; }
        public decimal StockValue { get; set; }  // CurrentQty × AvgRate
        public string? CurrentQtyDisplay { get; set; }
        public int Conversion { get; set; }
        public string? ExcessQtyDisplay { get; set; }
        public int? SourcePurchaseChallanId { get; set; }
        public decimal Cess { get; set; }
    }
}

