using AOne.Models;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.Entity
{
    public class ReceiveVoucher : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public required string VouncherNo { get; set; }
        public DateTime Date { get; set; }
        public Party? Party { get; set; } 
        public int? CustomerId { get; set; }  
        [ForeignKey("CustomerId")]
        public Customer? Customers { get; set; }
        public int? PaymentCategoryId { get; set; }
        [ForeignKey("PaymentCategoryId")]
        public PaymentVoucherCategory? PaymentCategory { get; set; }
        public decimal Amount { get; set; }
        public decimal GST { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string? Description { get; set; }
        public string? Attachments { get; set; }
        public string? TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
        public int? PaymentModeId { get; set; }
        [ForeignKey("PaymentModeId")]
        public ModeOfPayment? ModeOfPayment { get; set; }
        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
        public int? EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? RefNo { get; set; }
        public DateTime? ClearedDate { get; set; }
        public string? SelectedPurchaseIds { get; set; }
        public string? SelectedSalesIds { get; set; }
    }
}
