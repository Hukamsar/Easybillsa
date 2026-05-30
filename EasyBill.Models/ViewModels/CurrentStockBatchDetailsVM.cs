using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class CurrentStockBatchDetailsVM
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public int Conversion { get; set; }
        public List<CurrentStockBatchDetailsRowVM> Rows { get; set; } = new List<CurrentStockBatchDetailsRowVM>();
    }

    public class CurrentStockBatchDetailsRowVM
    {
        public string? Batch { get; set; }
        public decimal Stock { get; set; }
        public string? StockDisplay { get; set; }
        public string? Unit { get; set; }
        public decimal SalesRateA { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal Mrp { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? NavigateUrl { get; set; }
    }
}
