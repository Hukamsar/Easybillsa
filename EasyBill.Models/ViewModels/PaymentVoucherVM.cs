using AOne.Utility.Enums;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class PaymentVoucherVM
    {
        public int Id { get; set; }
        public  string VouncherNo { get; set; }
        public DateTime? Date { get; set; }
        public int? SupplierId { get; set; } 
        public int? VoucherCategoryId { get; set; }
        public string? VoucherCategoryName { get; set; }
        public decimal Amount { get; set; }  
        public decimal GST { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string? Description { get; set; }
        public string? Attachments { get; set; }
        //[SwaggerIgnore]
        public IFormFile? UploadAttachments { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? RefNo { get; set; }
        public Party? Party { get; set; }
        public int? CustomerId { get; set; } 
        public int? EmployeeId { get; set; }
        public int? PaymentModeId { get; set; }
        public List<int>? SelectedPurchaseIds { get; set; }
        public List<int>? SelectedSalesIds { get; set; }
        public List<string>? SelectedBillNos { get; set; }
    }
}
