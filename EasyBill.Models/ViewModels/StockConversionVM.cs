using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class StockConversionTargetRow
    {
        public int RetailItemId { get; set; }
        public decimal RetailQty { get; set; }
        public decimal WastageQty { get; set; }
        public decimal BulkQtyUsed { get; set; }
    }

    public class StockConversionPayload
    {
        public int BulkItemId { get; set; }
        public string Batch { get; set; } = null!;
        public string? Remarks { get; set; }
        public List<StockConversionTargetRow> Targets { get; set; } = new List<StockConversionTargetRow>();
    }
}
