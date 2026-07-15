using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class StockIssue : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customers { get; set; }

        public string? ChallanNo { get; set; }
        public DateTime? ChallanDate { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }

        [ValidateNever]
        public IList<StockIssueItem> StockIssuesItems { get; set; }

        [ValidateNever]
        public IList<SalsePaymentDetails>? SalsePaymentDetails { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public int? PharmacyDoctorId { get; set; }

        [ForeignKey(nameof(PharmacyDoctorId))]
        public PharmacyDoctor? PharmacyDoctor { get; set; }

        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? ReturnAmount { get; set; }
        public decimal? Balance { get; set; }
        public decimal? NetCollection { get; set; }
        public decimal? RoundOffAmount { get; set; }
        public decimal? TotalCessAmount { get; set; }
        public string? TaxCalculation { get; set; } = "Yes";

        // Branch Transfer tracking
        public string? TransferToTenantId { get; set; }
        public bool IsReceived { get; set; } = false;
        public string? TransferStatus { get; set; } = "Pending"; // Pending, Accepted, Rejected
    }
}
