using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class PurchaseChallan : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Suppliers { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public DateTime? BillDate { get; set; }
        public string PartyBillNo { get; set; } = string.Empty;
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
        public List<PurchaseChallanItem> PurchaseChallanItems { get; set; } = new();
        public decimal RoundOffAmount { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public string? PurchaseType { get; set; }
        public decimal TotalCGstAmt { get; set; }
        public decimal TotalSGstAmt { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal Balance { get; set; } = 0M;
        public IList<SalsePaymentDetails>? PaymentDetails { get; set; }
        public string Status { get; set; } = "Pending";
        public int? ConvertedPurchaseId { get; set; }
        [ForeignKey(nameof(ConvertedPurchaseId))]
        public Purchase? ConvertedPurchase { get; set; }
    }
}
