using AOne.Models.Entity;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class NearbyTenantResponse
    {
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Location { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Logo { get; set; }
        public string? DistanceText { get; set; }
        public double DistanceMeters { get; set; }
    }

    public class TenantStockCategoryResponse
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<TenantStockSubCategoryResponse> SubCategories { get; set; } = new List<TenantStockSubCategoryResponse>();
    }

    public class TenantStockSubCategoryResponse
    {
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public List<TenantStockItemResponse> Items { get; set; } = new List<TenantStockItemResponse>();
    }

    public class TenantStockItemResponse
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public decimal Mrp { get; set; }
        public decimal SalesRate { get; set; }
        public decimal TotalStockQuantity { get; set; }
        public List<string> ImageUrl { get; set; } = new();
        public string? Unit { get; set; }
        public string? Batch { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Barcode { get; set; }
        public decimal SalesRate1 { get; set; }
        public decimal SalesRate2 { get; set; }
        public int MinimumQty { get; set; }
        public int MaximumQty { get; set; }
        public decimal MaximumDiscount { get; set; }
        public bool DecemalAllowed { get; set; }
        public int Conversion { get; set; }
        public Hsn? Hsn { get; set; }
        public decimal Qty { get; set; }
        public decimal PurchaseRate { get; set; }
    }
}
