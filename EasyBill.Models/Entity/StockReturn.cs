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
    public class StockReturn : BaseEntity , IMayHaveTenant
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
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
        public IList<StockReturnItem> StockReturnItems { get; set; }
        public int? PharmacyDoctorId { get; set; }
        [ForeignKey(nameof(PharmacyDoctorId))]
        public PharmacyDoctor? PharmacyDoctor { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public decimal NetCollection { get; set; }
        public decimal RoundOffAmount { get; set; }
        public decimal TotalCessAmt { get; set; }
    }
}
