using System;

namespace EasyBill.Models.ViewModels
{
    public class PurchaseChallanVM : PurchaseVM
    {
        public string Status { get; set; } = "Pending";
        public int? ConvertedPurchaseId { get; set; }
        public bool IsConverted =>
            ConvertedPurchaseId.HasValue ||
            string.Equals(Status, "Converted", StringComparison.OrdinalIgnoreCase);
    }
}
