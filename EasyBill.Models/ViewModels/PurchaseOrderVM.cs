using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseOrderVM
    {
        public PurchaseOrderVM()
        {
            this.PurchaseOrderItemVMs = new List<PurchaseOrderItemVM>();
           // this.PaymentDetails = new List<SalsePaymentDetailsVM>();
        }
        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }  
        [Description("Bill No")]
        [Required(ErrorMessage = "BillNo is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "BillNo can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        public string? BillNo { get; set; } 
        public DateTime? BillDate { get; set; }
        public string? PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public decimal Total { get; set; }
        public decimal TotalGstAmt { get; set; }
        public decimal Totaldiscount { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal discountPercent { get; set; } = 0.0M;
        public decimal discountAmount { get; set; }
        public List<PurchaseOrderItemVM> PurchaseOrderItemVMs { get; set; }
        [Display(Name = "Round Off")]
        public decimal RoundOffAmount { get; set; }
        public object? TaxableAmount { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public string? PurchaseType { get; set; }
        public decimal TotalCGstAmt { get; set; }
        public decimal TotalSGstAmt { get; set; }
        public decimal TotalSCGstAmt { get; set; }
        public string? TenanatGst { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        public decimal Balance { get; set; } = 0M;
    }
}
