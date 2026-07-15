using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EasyBill.Models.ViewModels
{
    public class AddAddressRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(50)]
        public string Title { get; set; } = "Home";

        [Required(ErrorMessage = "Address Line is required")]
        [StringLength(250)]
        public string AddressLine { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? Pincode { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; }
    }

    public class UpdateAddressRequest : AddAddressRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid Address Id")]
        public int Id { get; set; }
    }

    public class CreateOrderRequest
    {
        [StringLength(50)]
        public string? OrderNumber { get; set; }
        
        public DateTime? OrderDate { get; set; }
        
        [Required(ErrorMessage = "Order address is required")]
        public string OrderAddress { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "TenantId is required")]
        public string? TenantId { get; set; }
        
        // Optional mobile number specific for this order
        [Phone(ErrorMessage = "Invalid Mobile Number")]
        public string? MobileNo { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
        public decimal TotalPayableAmount { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        public List<OrderLineItemRequest> Items { get; set; } = new List<OrderLineItemRequest>();
        
        public List<OrderPaymentRequest> PaymentDetails { get; set; } = new List<OrderPaymentRequest>();
    }

    public class OrderLineItemRequest
    {
        public int ItemId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public decimal Rate { get; set; }
        public decimal Qty { get; set; }
        public decimal GstPercent { get; set; }
        public decimal GstAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class OrderPaymentRequest
    {
        public int PaymentModeId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}
