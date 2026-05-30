using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SalesVM
    {
        public SalesVM()
        {
            SalesItemVMs = new List<SalesItemVM>();
            SalsePaymentDetails = new List<SalsePaymentDetailsVM>();
        }
        public int Id { get; set; }

        [Description("Bill No")]
        [Required(ErrorMessage = "BillNo is required.")]
        [RegularExpression(@"^(?!\s)[A-Za-z0-9 /-]+(?<!\s)$", ErrorMessage = "BillNo can contain only letters, numbers, spaces, hyphens, and slashes. It cannot start or end with a space.")]
        public string? BillNo { get; set; }

        [Description("Bill Date")]
        public DateTime? BillDate { get; set; } 
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? MobileNo { get; set; }
        public string? Address { get; set; }
        public IList<SalesItemVM>? SalesItemVMs { get; set; }
        public IList<SalsePaymentDetailsVM>? SalsePaymentDetails { get; set; }
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
        public decimal Balance { get; set; } = 0M;
        // Add this property for SalesOrder relationship
        public int? SalesOrderId { get; set; }
        public int? OfferId { get; set; }
        public string? billingType { get; set; }
        public string? PaymentType { get; set; }
        public int HoldId { get; set; }        // Resume ke time use hoga
        public bool IsHoldMode { get; set; }    // UI behavior ke liye
        public string? ItemName { get; set; }
        public decimal TotalQty { get; set; }
        public string? QtyDisplay { get; set; }
        public decimal NetCollection { get; set; }
        [Display(Name = "Round Off")]
        public decimal RoundOffAmount { get; set; }
        public List<SalesVM> ChildPayments { get; set; } = new List<SalesVM>();
        public decimal TotalCessAmount { get; set; }
        public int Qty { get; set; }
        public string? Mfg { get; set; }
        public DateTime? ExpiryDate { get; set; }

        #region StockInSalesStateMent Report
        public string? Packing { get; set; }

        public decimal OpeningQty { get; set; }
        public decimal OpeningAmount { get; set; }

        public decimal PurchaseQty { get; set; }
        public decimal PurchaseAmount { get; set; }

        public decimal SalesQty { get; set; }
        public decimal SalesAmount { get; set; }

        public decimal BalanceQty { get; set; }
        public decimal PurchaseReturnQty { get; set; }
        public decimal SalesReturnQty { get; set; }
        public decimal PurchaseReturnAmount { get; set; }
        public decimal SalesReturnAmount { get; set; }
        public decimal ClosingQty { get; set; }
        public decimal ClosingAmount { get; set; }
        public string? PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public string? Type { get; set; }
        public decimal ClosingGstAmount { get; set; }    
        public decimal ClosingCessAmount { get; set; }
        public decimal SalesReturnGstAmount { get; set; }     
        public decimal SalesReturnCessAmount { get; set; }
        public decimal SalesGstAmount { get; set; }      
        public decimal SalesCessAmount { get; set; }
        public decimal PurchaseReturnGstAmount { get; set; }   
        public decimal PurchaseReturnCessAmount { get; set; }
        public decimal PurchaseGstAmount { get; set; }   
        public decimal PurchaseCessAmount { get; set; }
        public decimal OpeningGstAmount { get; set; }     
        public decimal OpeningCessAmount { get; set; }    
                                                         
        public List<SalesVM>? Invoices { get; set; }
        public string? Batch { get; set; }
        public decimal Rate { get; set; }
        public bool HasNarcoticItem { get; set; }
        #endregion
    }

}
