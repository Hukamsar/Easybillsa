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
        [Required(ErrorMessage = "PO No is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "PO No can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        [Display(Name = "PO No.")]
        public string? BillNo { get; set; } 
        [Display(Name = "PO Date")]
        public DateTime? BillDate { get; set; }
        [Display(Name = "Party Ref No.")]
        public string? PartyBillNo { get; set; }
        [Display(Name = "Party Ref Date")]
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
        [Display(Name = "Type")]
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

        // Hub and Spoke Additions
        public string? TargetTenantId { get; set; } 
        public string? OrderType { get; set; } // "PO" or "SR"
        public string? WorkflowStatus { get; set; } 
        public int? ParentPurchaseOrderId { get; set; } 
        public string? DeliveryType { get; set; } 
    }
}
