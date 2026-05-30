using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.ViewModels
{
    public class StockIssueVM
    {
        public StockIssueVM()
        {
            StockIssuesItemVMs = new List<StockIssueItemVM>();
            SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
        }

        public int Id { get; set; }

        [Description("Challan No")]
        [Required(ErrorMessage = "ChallanNo is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "ChallanNo can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        public string? ChallanNo { get; set; }

        public DateTime? ChallanDate { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public IList<StockIssueItemVM> StockIssuesItemVMs { get; set; }
        public IList<SalsePaymentDetailsVM>? SalsePaymentDetails { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }

        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }

        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }

        [NotMapped]
        public bool DoctorRequired { get; set; }

        public int? PharmacyDoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }
        public decimal? Cess { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? ReturnAmount { get; set; }
        public decimal? Balance { get; set; } = 0M;
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public decimal? NetCollection { get; set; }

        [Display(Name = "Round Off")]
        public decimal? RoundOffAmount { get; set; }

        public decimal? TotalCessAmount { get; set; }
        public string? TaxCalculation { get; set; } = "Yes";
    }
}
