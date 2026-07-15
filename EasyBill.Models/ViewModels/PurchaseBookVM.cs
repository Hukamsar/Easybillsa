using System;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseBookVM
    {
        public DateTime? Date { get; set; }
        public string InvNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal BillValue { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal TaxValue { get; set; }
        public decimal TaxFreeValue { get; set; }
    }
}
