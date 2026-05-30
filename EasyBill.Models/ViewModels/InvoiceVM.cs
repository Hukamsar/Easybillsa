using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.Models.ViewModels
{
    public class InvoiceVM
    {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string InvoiceNo { get; set; }
        public string? PhoneNo { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Total { get; set; }
        public decimal discountper { get; set; }
        public decimal discountvalue { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public string? Address { get; set; }
        public List<InvoiceItemVM> invoiceItemVms { get; set; }
    }
}
