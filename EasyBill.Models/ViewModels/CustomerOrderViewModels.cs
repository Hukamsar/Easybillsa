using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class AddAddressRequest
    {
        public string Title { get; set; } = "Home";
        public string AddressLine { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
        public bool IsDefault { get; set; }
    }

    public class CreateOrderRequest
    {
        public string? OrderNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string OrderAddress { get; set; } = string.Empty;
        
        // Optional mobile number specific for this order
        public string? MobileNo { get; set; }

        public decimal TotalPayableAmount { get; set; }

        public List<OrderLineItemRequest> Items { get; set; } = new List<OrderLineItemRequest>();
        public List<OrderPaymentRequest>? PaymentDetails { get; set; } = new List<OrderPaymentRequest>();
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
