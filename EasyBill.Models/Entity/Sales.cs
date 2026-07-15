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
    public class Sales : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }


        [ForeignKey(nameof(CustomerId))]
        public Customer? Customers { get; set; }

        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }
        [ValidateNever]
        public IList<SalesItem> SalesItems { get; set; }
        [ValidateNever]
        public IList<SalsePaymentDetails> SalsePaymentDetails { get; set; }
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
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public int? OfferId { get; set; }
        [ForeignKey(nameof(OfferId))]
        public Offer? Offer { get; set; }

        public int? SalesOrderId { get; set; }

        [ForeignKey(nameof(SalesOrderId))]
        public SalesOrder? SalesOrder { get; set; }
        public decimal Balance { get; set; }
        public decimal NetCollection { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal TotalCessAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Remarks { get; set; }
    }
}
