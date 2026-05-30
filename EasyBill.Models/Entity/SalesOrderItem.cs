using AOne.Models.Entity;
using AOne.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class SalesOrderItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        [ForeignKey(nameof(SalesOrderId))]
        public SalesOrder? SalesOrder { get; set; }
        public int ItemMasterId { get; set; }
        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster? ItemMaster { get; set; }
        public int? PurchaseItemId { get; set; }
        [ForeignKey(nameof(PurchaseItemId))]
        public PurchaseItem? PurchaseItem { get; set; }
        public string? Batch { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public decimal StripRate { get; set; }
        public decimal Cess { get; set; }
    }
}
