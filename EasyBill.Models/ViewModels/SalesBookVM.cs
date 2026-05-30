using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class SalesBookVM
    {
        public DateTime? Date { get; set; }
        public string InvNo { get; set; }
        public string Customer { get; set; }
        public string Type { get; set; } // Cash / Credit
        public decimal TaxableAmt { get; set; }
        public decimal TaxValue { get; set; }
        public decimal TotalAmt { get; set; }
        public string PaymentMode { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal RoundOff { get; set; }
    }

}
