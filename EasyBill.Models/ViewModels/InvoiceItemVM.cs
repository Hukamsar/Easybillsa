using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class InvoiceItemVM
    {
        public int Id { get; set; }
        public int ItemId { get; set; } 
        public string ItemName { get; set; }
        public int InvoiceId { get; set; } 
        public string InvoiceCode { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public string? HsnCode { get; set; }
        public decimal Gst { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
