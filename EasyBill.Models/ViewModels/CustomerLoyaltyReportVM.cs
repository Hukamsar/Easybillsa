using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class CustomerLoyaltyReportVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? MobileNo { get; set; }
        public string? Email { get; set; }

        public decimal CashbackEarned { get; set; }
        public decimal CashbackUsed { get; set; }
        public decimal CashbackBalance { get; set; }
        public decimal CashbackOverUsed { get; set; }
        public string CashbackUsageStatus { get; set; } = "No cashback earned yet.";
        public bool CashbackNeverExpires { get; set; } = true;

        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
        public int PointsBalance { get; set; }
        public decimal PointValueInRs { get; set; }
        public decimal PointsBalanceValueInRs { get; set; }
        public bool IsPointProgramActive { get; set; }
        public bool AllowPointRedemption { get; set; }
        public bool IsCustomerGettingPoints { get; set; }
        public int MinPointsToRedeem { get; set; }

        public List<CashbackLedgerItemVM> CashbackLedger { get; set; } = new();
        public List<PointLedgerItemVM> PointLedger { get; set; } = new();
    }

    public class CashbackLedgerItemVM
    {
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int? SaleId { get; set; }
        public string? BillNo { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool NeverExpires { get; set; } = true;
        public string? Remarks { get; set; }
    }

    public class PointLedgerItemVM
    {
        public DateTime TransactionDate { get; set; }
        public int EarnedPoints { get; set; }
        public int RedeemedPoints { get; set; }
        public int NetPoints { get; set; }
        public decimal PointValueInRs { get; set; }
        public decimal NetValueInRs { get; set; }
        public int? SaleId { get; set; }
        public string? BillNo { get; set; }
    }

    public class CustomerPointSummaryVM
    {
        public int CustomerId { get; set; }
        public int PointsBalance { get; set; }
        public decimal PointValueInRs { get; set; }
        public decimal PointsBalanceValueInRs { get; set; }
        public bool IsPointProgramActive { get; set; }
        public bool AllowPointRedemption { get; set; }
        public int MinPointsToRedeem { get; set; }
    }

    public class PointApplicationResultVM
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int EarnedPoints { get; set; }
        public int RedeemedPoints { get; set; }
        public decimal RedeemedAmount { get; set; }
        public bool SaleUpdated { get; set; }
    }
}
