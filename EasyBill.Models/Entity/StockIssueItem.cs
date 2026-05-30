using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class StockIssueItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int StockIssueId { get; set; }

        [ForeignKey(nameof(StockIssueId))]
        public StockIssue? StockIssue { get; set; }

        public int ItemMasterId { get; set; }

        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster? ItemMaster { get; set; }

        public int? HsnId { get; set; }

        [ForeignKey(nameof(HsnId))]
        public Hsn? Hsn { get; set; }

        public string? HsnCode { get; set; }
        public int? PurchaseItemId { get; set; }

        [ForeignKey(nameof(PurchaseItemId))]
        public PurchaseItem? PurchaseItem { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public string? Batch { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal? IGst { get; set; }
        public decimal? CGst { get; set; }
        public decimal? SGst { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }
        public bool IsSoldInTablets { get; set; }
        public decimal? StripRate { get; set; }
        public decimal? Cess { get; set; }
    }
}
