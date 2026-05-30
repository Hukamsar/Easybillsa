using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class InvoiceItem : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int ItemId {  get; set; }
        [ForeignKey(nameof(ItemId))]
        public ItemMaster ItemMaster { get; set; }
        public int InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public Invoice Invoice { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public string? HsnCode { get; set; }
        public decimal Gst { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
    }
}
