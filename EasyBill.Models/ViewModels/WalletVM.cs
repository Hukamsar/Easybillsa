using AOne.Utility.Enums;

namespace EasyBill.Models.ViewModels
{
    public class WalletMasterVM
    {
        public int Id { get; set; }
        public string WalletID { get; set; } = string.Empty;
        public WalletOwnerType WalletType { get; set; } = WalletOwnerType.CustomerWallet;
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? DeliveryBoyId { get; set; }
        public string? DeliveryBoyName { get; set; }
        public string? DeliveryBoyMobileNo { get; set; }
        public decimal CurrentBalance { get; set; }
        public WalletStatus Status { get; set; } = WalletStatus.Active;
        public decimal? PerBillUsageLimit { get; set; }
        public bool AutoDeductInBilling { get; set; } = true;
        public bool IsExpiryEnabled { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class WalletTransactionVM
    {
        public int Id { get; set; }
        public int WalletMasterId { get; set; }
        public string WalletID { get; set; } = string.Empty;
        public DateTime TransactionDateTime { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public WalletTransactionType TransactionType { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ReferenceBillNo { get; set; }
        public int? ReferenceSaleId { get; set; }
        public string? UserName { get; set; }
        public string? Remarks { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
    }

    public class WalletDashboardVM
    {
        public int WalletMasterId { get; set; }
        public string WalletID { get; set; } = string.Empty;
        public WalletOwnerType WalletType { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public List<WalletTransactionVM> LastTransactions { get; set; } = new();
    }

    public class WalletMasterRequestVM
    {
        public WalletOwnerType WalletType { get; set; } = WalletOwnerType.CustomerWallet;
        public int? CustomerId { get; set; }
        public int? DeliveryBoyId { get; set; }
        public string? DeliveryBoyName { get; set; }
        public string? DeliveryBoyMobileNo { get; set; }
        public decimal OpeningBalance { get; set; }
        public WalletStatus Status { get; set; } = WalletStatus.Active;
        public decimal? PerBillUsageLimit { get; set; }
        public bool AutoDeductInBilling { get; set; } = true;
        public bool IsExpiryEnabled { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    public class WalletAdjustmentRequestVM
    {
        public int WalletMasterId { get; set; }
        public decimal Amount { get; set; }
        public bool IsCredit { get; set; }
        public WalletTransactionType TransactionType { get; set; } = WalletTransactionType.ManualCredit;
        public string? PaymentMode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ReferenceBillNo { get; set; }
        public int? ReferenceSaleId { get; set; }
        public string? Remarks { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class WalletLedgerFilterVM
    {
        public int? WalletMasterId { get; set; }
        public int? CustomerId { get; set; }
        public WalletOwnerType? WalletType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }

    public class WalletSaleApplyRequestVM
    {
        public int CustomerId { get; set; }
        public decimal SaleTotalPayable { get; set; }
        public decimal ExistingBalance { get; set; }
        public decimal RequestedUseAmount { get; set; }
        public bool UseFullWallet { get; set; }
        public bool AutoDeductWallet { get; set; }
        public string? BillNo { get; set; }
        public int? SaleId { get; set; }
    }

    public class WalletSaleApplyResultVM
    {
        public bool Success { get; set; }
        public decimal WalletDeductedAmount { get; set; }
        public decimal RemainingBalance { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class WalletReportVM
    {
        public List<WalletMasterVM> BalanceReport { get; set; } = new();
        public List<WalletTransactionVM> StatementReport { get; set; } = new();
        public List<WalletTransactionVM> ExpiredBalanceReport { get; set; } = new();
        public List<WalletTransactionVM> RefundToWalletReport { get; set; } = new();
        public List<WalletTransactionVM> UsageReport { get; set; } = new();
        public decimal TotalLiability { get; set; }
    }

    public class WalletSettingsRequest
    {
        public decimal? WalletBalance { get; set; }
        public decimal? WhatsAppMessageCharge { get; set; }
        public decimal? SmsMessageCharge { get; set; }
        public decimal? EmailMessageCharge { get; set; }
        public string? PaymentGatewayProvider { get; set; }
        public string? PaymentGatewayKey { get; set; }
        public string? PaymentGatewaySecret { get; set; }
        public bool? IsPaymentGatewayActive { get; set; }
        public bool? IsWalletActive { get; set; }
        public bool IsSmsChargeActive { get; set; } 
        public bool IsEmailChargeActive { get; set; } 
        public bool IsWhatsAppChargeActive { get; set; }
    }

    public class WalletRechargeRequest
    {
        public decimal Amount { get; set; }
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public string? ServiceType { get; set; }
        public decimal? ServiceCharge { get; set; }
    }

    public class GatewayRechargeConfirmRequest
    {
        public string? GatewayOrderId { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? GatewaySignature { get; set; }
    }

    public class PendingGatewayOrder
    {
        public decimal Amount { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string? ServiceType { get; set; }
        public decimal? ServiceCharge { get; set; }
        public string? Remarks { get; set; }
        public string? ReferenceNo { get; set; }
    }

    public class GatewayConfig
    {
        public string Provider { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }
    }

    public class RechargeHistoryRow
    {
        public int Id { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public decimal Amount { get; set; }
        public bool IsDebit { get; set; }
        public string? PaymentMode { get; set; }
        public string? ReferenceNo { get; set; }
        public string? ServiceType { get; set; }
        public decimal? ServiceCharge { get; set; }
        public string? Note { get; set; }
    }
}
