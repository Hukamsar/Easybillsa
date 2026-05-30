using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.Entity;
using AOne.Models;

namespace EasyBill.Models.Entity
{
    public class HoldSalesItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }

        public int HoldSalesId { get; set; }
        [ForeignKey(nameof(HoldSalesId))]
        public HoldSales? HoldSales { get; set; }

        public int ItemMasterId { get; set; }
        [ForeignKey(nameof(ItemMasterId))]
        public ItemMaster? ItemMaster { get; set; }

        public int? PurchaseItemId { get; set; }

        public string? Batch { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }

        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal Discount { get; set; }
        public decimal Amount { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
