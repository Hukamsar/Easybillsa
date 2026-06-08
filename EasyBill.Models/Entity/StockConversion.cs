using System;
using System.ComponentModel.DataAnnotations.Schema;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class StockConversion : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public string VoucherNo { get; set; } = null!;
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public int BulkItemId { get; set; }
        [ForeignKey(nameof(BulkItemId))]
        public ItemMaster? BulkItem { get; set; }
        public decimal BulkQty { get; set; }

        public int RetailItemId { get; set; }
        [ForeignKey(nameof(RetailItemId))]
        public ItemMaster? RetailItem { get; set; }
        public decimal RetailQty { get; set; }

        public decimal WastageQty { get; set; }
        public string? Remarks { get; set; }

        public string? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
