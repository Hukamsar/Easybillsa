namespace EasyBill.Models.ViewModels
{
    public class StockIssueItemVM
    {
        public int Id { get; set; }
        public int StockIssueId { get; set; }
        public int ItemMasterId { get; set; }
        public int? HsnId { get; set; }
        public string? HsnCode { get; set; }
        public string? ItemName { get; set; }
        public string? ItemBarcodeNumber { get; set; }
        public int? PurchaseItemId { get; set; }
        public string? Batch { get; set; }
        public decimal Qty { get; set; }
        public decimal TabletQty { get; set; }
        public decimal Rate { get; set; }
        public decimal Gst { get; set; }
        public decimal? IGst { get; set; }
        public decimal? CGst { get; set; }
        public decimal? SGst { get; set; }
        public decimal Discount { get; set; } = 0M;
        public decimal Amount { get; set; }
        public DateTime? Expirydate { get; set; }
        public decimal Mrp { get; set; }
        public bool IsSoldInTablets { get; set; }
        public decimal? StripRate { get; set; }
        public decimal? Cess { get; set; }
    }
}
