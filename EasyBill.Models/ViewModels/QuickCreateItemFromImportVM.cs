namespace EasyBill.Models.ViewModels
{
    public class QuickCreateItemFromImportVM : ItemMasterVM
    {
        public string? RowKey { get; set; }

        public string? SourceItemName { get; set; }

        public string? SourceBatch { get; set; }

        public int? SourceQty { get; set; }

        public int? SourceFreeQty { get; set; }

        public decimal? SourceRate { get; set; }

        public decimal? SourceGst { get; set; }

        public decimal? SourceCgst { get; set; }

        public decimal? SourceSgst { get; set; }

        public decimal? SourceCess { get; set; }
    }
}
