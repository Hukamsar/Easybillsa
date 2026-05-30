using AOne.Models;
using AOne.Models.Entity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyBill.Models.Entity
{
    public class SalsePaymentDetails : BaseEntity, IMayHaveTenant
    {
        public int Id { get; set; }
        public int PaymentModeId { get; set; }

        [ForeignKey(nameof(PaymentModeId))]
        public ModeOfPayment ModeOfPayment { get; set; }

        public decimal Amount { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Description { get; set; }
        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; }

        public int? SalseId { get; set; }

        [ForeignKey(nameof(SalseId))]
        public Sales? Sales { get; set; }

        public Tenant? Tenant { get; set; }
        public string? TenantId { get; set; }
        public int? SalesOrderId { get; set; }

        [ForeignKey(nameof(SalesOrderId))]
        public SalesOrder? SalesOrder { get; set; }

        public DateTime? Date { get; set; }
        public int? StockReturnId { get; set; }

        [ForeignKey("StockReturnId")]
        public StockReturn? StockReturn { get; set; }

        public int? StockIssueId { get; set; }

        [ForeignKey("StockIssueId")]
        public StockIssue? StockIssue { get; set; }

        public int? PurchaseId { get; set; }

        [ForeignKey("PurchaseId")]
        public Purchase? Purchase { get; set; }

        public int? PurchaseChallanId { get; set; }

        [ForeignKey("PurchaseChallanId")]
        public PurchaseChallan? PurchaseChallan { get; set; }

        public int? StockReceiveId { get; set; }

        [ForeignKey("StockReceiveId")]
        public StockReceive? StockReceive { get; set; }
    }
}
