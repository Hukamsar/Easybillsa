using System;

namespace EasyBill.Models.ViewModels
{
    public class Gstr2DetailVM
    {
        public int PurchaseId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string GSTIN { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public DateTime? BillDate { get; set; }
        public string SupplyType { get; set; } = string.Empty;
        public string HSN { get; set; } = string.Empty;
        public string QtyDisplay { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal InvoiceValue { get; set; }
        public decimal Taxable { get; set; }
        public decimal SGSTPer { get; set; }
        public decimal SGST { get; set; }
        public decimal CGSTPer { get; set; }
        public decimal CGST { get; set; }
        public decimal IGSTPer { get; set; }
        public decimal IGST { get; set; }
        public decimal Cess { get; set; }
        public decimal TotalGST { get; set; }
    }
}
