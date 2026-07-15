using AOne.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.Entity;

namespace EasyBill.Models.Entity
{
    public class StockReceive : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customers { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }
        public string? ChallanNo { get; set; }
        public DateTime? ChallanDate { get; set; }
        public string? PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        [ValidateNever]
        public IList<StockReceiveItem> StockReceiveItems { get; set; } = new List<StockReceiveItem>();
        [ValidateNever]
        public IList<SalsePaymentDetails>? PaymentDetails { get; set; } = new List<SalsePaymentDetails>();
        public int? PharmacyDoctorId { get; set; }
        [ForeignKey(nameof(PharmacyDoctorId))]
        public PharmacyDoctor? PharmacyDoctor { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalCessAmount { get; set; }
        public string? TaxCalculation { get; set; } = "Yes";

        // Branch Transfer tracking
        public string? TransferFromTenantId { get; set; }
        public bool IsPendingTransfer { get; set; } = false;
        public int? SourceStockIssueId { get; set; }
    }
}
