using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class StockVM
    {
        public int Id { get; set; }
        public int ItemMasterId { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public List<string> ItemImages { get; set; } = new();
        public decimal Stocks { get; set; }
        public string? Unit1 { get; set; }
        public string? Unit2 { get; set; }
        public decimal? Amount { get; set; }
        public decimal? GstAmount { get; set; }

        //Add New Fields
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public decimal Qty { get; set; }
        public decimal Mrp { get; set; }
        public decimal Rate { get; set; }
        public decimal SalesRateA { get; set; }
        public decimal SalesRateB { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int MinQty { get; set; }
        public int DaysSincePurchase { get; set; }
        public int DaysSinceLastSale { get; set; } 
        public DateTime? ExpiryDate { get; set; }
        public string? Batch { get; set; }
        public int MaxQty { get; set; }

        public string? StockDisplay { get; set; }
        public int Conversion { get; set; }
        public string? SupplierName { get; set; }
        public string? Packing { get; set; }

    }
}
