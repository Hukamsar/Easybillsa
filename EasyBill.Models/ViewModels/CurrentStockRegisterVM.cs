using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class CurrentStockRegisterVM
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public int Conversion { get; set; }
        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? Mrp { get; set; }
        public decimal CurrentStock { get; set; }
        public string? CurrentStockDisplay { get; set; }
        public List<CurrentStockRegisterRowVM> Rows { get; set; } = new List<CurrentStockRegisterRowVM>();
    }

    public class CurrentStockRegisterRowVM
    {
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string? Particulars { get; set; }
        public decimal Receive { get; set; }
        public string? ReceiveDisplay { get; set; }
        public decimal Issue { get; set; }
        public string? IssueDisplay { get; set; }
        public decimal Balance { get; set; }
        public string? BalanceDisplay { get; set; }
        public string? SourceType { get; set; }
        public int SourceId { get; set; }
        public string? NavigateUrl { get; set; }
        public string Type { get; set; }
    }
}
