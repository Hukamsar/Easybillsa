using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EasyBill.Models.Entity
{
    public class Purchase : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; } 
        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Suppliers { get; set; } 
        public string BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string PartyBillNo { get; set; }
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
        public List<PurchaseItem> PurchaseItems { get; set; }
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
        public int? PurchaseOrderId { get; set; }
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }
        public decimal TotalCessAmt {  get; set; }
        public decimal Expense { get; set; }
        public string? Remarks { get; set; }
    }
}
