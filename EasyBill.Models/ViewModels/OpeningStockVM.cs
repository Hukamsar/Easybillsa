using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class OpeningStockVM
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public int? ItemId { get; set; } 
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? Packing { get; set; }
        public bool IsSelected { get; set; }
        public string? Batch { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }
        public decimal RateA { get; set; }
        public decimal RateB { get; set; }
        public decimal Qty { get; set; }
        public List<OpeningStockBatchVM> Batches { get; set; } = new();
    }
    public class OpeningStockBatchVM
    {
        public string Batch { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Qty { get; set; }
        public decimal Mrp { get; set; }
        public decimal RateA { get; set; }
        public decimal RateB { get; set; }
    }
}
