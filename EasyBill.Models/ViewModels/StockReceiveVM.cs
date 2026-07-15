using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.ViewModels
{
    public class StockReceiveVM
    {
        public StockReceiveVM()
        {
            StockReceiveItemVMs = new List<StockReceiveItemVM>();
            PaymentDetails = new List<SalsePaymentDetailsVM>();
        }

        public int Id { get; set; }

        [Description("Challan No")]
        [Required(ErrorMessage = "ChallanNo is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "ChallanNo can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        public string? ChallanNo { get; set; }
        public DateTime? ChallanDate { get; set; }
        [Display(Name = "Party Bill No")]
        public string? PartyBillNo { get; set; }
        [Display(Name = "Party Bill Date")]
        public DateTime? PartyBillDate { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public IList<StockReceiveItemVM> StockReceiveItemVMs { get; set; }
        public IList<SalsePaymentDetailsVM>? PaymentDetails { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalPayable { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public decimal RoundOffAmount { get; set; }
        public string? PurchaseType { get; set; } = "Local";
        public decimal TotalCGstAmt { get; set; }
        public decimal TotalSGstAmt { get; set; }
        public decimal TotalSCGstAmt { get; set; }
        public string? TenanatGst { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal Balance { get; set; } = 0M;
        public decimal TotalCessAmount { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public string? TaxCalculation { get; set; } = "Yes";
        public int? PurchaseOrderId { get; set; }
        public int? SourcePurchaseChallanId { get; set; }
        public int? SourceStockIssueId { get; set; }
        public int? PharmacyDoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? DoctorMobileNumber { get; set; }
        public string? DoctorRegNumber { get; set; }

        // Branch Transfer additions
        public string? ReceiveFromType { get; set; } = "Supplier"; // "Supplier" or "Branch"
        public string? TransferFromTenantId { get; set; }
        public bool IsPendingTransfer { get; set; } = false;
        public string? TransferFromBranchName { get; set; }
    }
}
