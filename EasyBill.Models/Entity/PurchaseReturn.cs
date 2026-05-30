using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class PurchaseReturn : BaseEntity,IMayHaveTenant
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Suppliers { get; set; }
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public Purchasereturnreason? Reason { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public decimal TotalGstAmt { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal discountPercent { get; set; }
        public decimal discountAmount { get; set; }
        public decimal Total { get; set; }
        [ValidateNever]
        public List<PurchaseReturnItem> PurchaseReturnItems { get; set; } = new List<PurchaseReturnItem>();
        public decimal RoundOffAmount { get; set; }
        public decimal TotalCessAmt { get; set; }
    }
}
