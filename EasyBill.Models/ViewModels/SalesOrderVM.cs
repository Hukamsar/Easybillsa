using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SalesOrderVM
    {
        public SalesOrderVM()
        {
            SalesOrderItemVMs = new List<SalesOrderItemVM>();
            SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
            OpticalVMs = new List<OpticalVM>();
        }
        public int Id { get; set; }

        [Description("Bill No")]
        [Required(ErrorMessage = "BillNo is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "BillNo can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        public string? BillNo { get; set; }

        [Description("Bill Date")]
        public DateTime? BillDate { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public IList<SalesOrderItemVM>? SalesOrderItemVMs { get; set; }
        public IList<SalsePaymentDetailsVM>? SalsePaymentDetails { get; set; }
        public IList<OpticalVM>? OpticalVMs { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public int? PharmacyDoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }
        public bool DoctorRequired { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Tenantname { get; set; }
        public string? TenantAddress { get; set; }
        public string? TenantPhone { get; set; }
        public string? TenantEmail { get; set; }
        public string? TermConditions { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public int? OfferId { get; set; }
        public decimal Balance { get; set; } = 0M;
        public string? billingType { get; set; }
        [Display(Name = "Round Off")]
        public decimal RoundOffAmount { get; set; }
        public decimal TotalCessAmt { get; set; }
    }
}
