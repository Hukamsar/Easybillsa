namespace EasyBill.Models.ViewModels
{
    public class PurchaseImportPreviewVM
    {
        public string DetectedFormat { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public PurchaseImportHeaderVM Header { get; set; } = new();
        public List<PurchaseImportItemVM> Items { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class PurchaseImportHeaderVM
    {
        public string? BillNo { get; set; }
        public DateTime? BillDate { get; set; }
        public string? PartyBillNo { get; set; }
        public DateTime? PartyBillDate { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? PurchaseType { get; set; }
    }

    public class PurchaseImportItemVM
    {
        public int? ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Batch { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public int Qty { get; set; }
        public int FreeQty { get; set; }
        public decimal Rate { get; set; }
        public decimal Mrp { get; set; }
        public decimal Discount { get; set; }
        public decimal Gst { get; set; }
        public decimal CGst { get; set; }
        public decimal SGst { get; set; }
        public decimal SCGst { get; set; }
        public decimal Cess { get; set; }
        public int? HsnId { get; set; }
        public decimal SalserateA { get; set; }
        public decimal SalserateB { get; set; }
        public decimal BatchWiseCose { get; set; }
        public string? Barcode { get; set; }
        public bool IsResolved { get; set; } = true;
        public string? ResolutionMessage { get; set; }
    }
}
