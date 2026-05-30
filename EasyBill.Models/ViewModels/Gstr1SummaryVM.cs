using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class Gstr1SummaryVM
    {
        public string Key { get; set; }   
        public int Count { get; set; }
        public decimal Taxable { get; set; }
        public decimal SGST { get; set; }
        public decimal CGST { get; set; }
        public decimal IGST { get; set; }
        public decimal Cess { get; set; }
        public decimal InvoiceAmount { get; set; }

        public decimal TotalGST => SGST + CGST + IGST + Cess;
    }
}
