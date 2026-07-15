using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class PurchaseOrder : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Suppliers { get; set; }
        [Display(Name = "PO No.")]
        public string BillNo { get; set; }
        [Display(Name = "PO Date")]
        public DateTime? BillDate { get; set; }
        [Display(Name = "Party Ref No.")]
        public string? PartyBillNo { get; set; }
        [Display(Name = "Party Ref Date")]
        public DateTime? PartyBillDate { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public decimal TotalGstAmt { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal discountPercent { get; set; }
        public decimal discountAmount { get; set; }
        public decimal Total { get; set; }
        public decimal PaymentAmt { get; set; }
        public string? PaymentStatus { get; set; }
        [ValidateNever]
        public List<PurchaseOrderItem>? PurchaseOrderItems { get; set; }
        public decimal RoundOffAmount { get; set; }
        [Display(Name = "Type")]
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public string? PurchaseType { get; set; }
        public decimal TotalCGstAmt { get; set; }
        public decimal TotalSGstAmt { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal Balance { get; set; } = 0M;

        // Hub and Spoke Additions
        public string? TargetTenantId { get; set; } 
        [ForeignKey(nameof(TargetTenantId))]
        public Tenant? TargetTenant { get; set; }
        public string? OrderType { get; set; } // "PO" or "SR"
        public string? WorkflowStatus { get; set; } 
        public int? ParentPurchaseOrderId { get; set; } 
        public string? DeliveryType { get; set; } 
    }
}
