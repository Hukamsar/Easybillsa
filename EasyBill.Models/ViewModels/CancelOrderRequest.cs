namespace EasyBill.Models.ViewModels
{
    /// <summary>
    /// Request body for Cancel Order API.
    /// Reason field optional hai — customer explain kar sake ki kyun cancel kar raha hai.
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>
        /// Optional reason for cancellation.
        /// e.g. "Changed my mind", "Ordered by mistake", "Delivery delay"
        /// </summary>
        public string? Reason { get; set; }
    }
}
