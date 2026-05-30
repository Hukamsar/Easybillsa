using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models;
using AOne.Models.Entity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EasyBill.Models.Entity
{
    public class Invoice : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }
        public string? PhoneNo { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string? Address { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Total { get; set; }
        public decimal discountper { get; set; }
        public decimal discountvalue { get; set; }
        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        [ValidateNever]
        public IList<InvoiceItem> invoiceItems { get; set; }
    }
}
