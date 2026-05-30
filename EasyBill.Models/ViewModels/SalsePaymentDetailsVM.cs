namespace EasyBill.Models.ViewModels
{
    public class SalsePaymentDetailsVM
    {
        public int Id { get; set; }
        public int PaymentModeId { get; set; }
        public string? PaymentModeName { get; set; }
        public decimal Amount { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Description { get; set; }
        public int? CustomerId { get; set; }
        public int? SalseId { get; set; }
        public int? StockIssueId { get; set; }
        public int? StockReceiveId { get; set; }
    }
}
