using System;

namespace EasyBill.Models.ViewModels
{
    public class Gstr2SummaryVM
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
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
