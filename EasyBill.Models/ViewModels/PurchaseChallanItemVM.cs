using System;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.ViewModels
{
    public class PurchaseChallanItemVM
    {
        // Item Details
        [Required(ErrorMessage = "Please choose the Item")]
        public int ItemId { get; set; }
        public string? ItemName { get; set; } 

        public string? Description { get; set; } 

        [Required]
        public decimal Qty { get; set; } 

        [Required]
        public decimal Rate { get; set; }

        // Discount and GST details from your UI grid
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }

        public decimal Amount { get; set; } 

        public decimal ConvertedQty { get; set; }
        public decimal PendingQty => Qty - ConvertedQty;
    }
}