using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class CurrentStockVM
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        [ForeignKey("ItemId")]
        public ItemMaster ItemMaster { get; set; }
        public string? Batch { get; set; } 
        public DateTime? ExpiryDate { get; set; }
        public decimal Qty { get; set; }
        public decimal PurchaseRate { get; set; }
        public decimal Mrp { get; set; }
        public decimal SalesRateA { get; set; }
        public decimal SalesRateB { get; set; }
        public string? Barcode { get; set; }
        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
