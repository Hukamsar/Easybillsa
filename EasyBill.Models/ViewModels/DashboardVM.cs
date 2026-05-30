using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.Models.ViewModels
{
    public class DashboardVM
    {
        public decimal TotalSales { get; set; }
        public decimal ThisMonthSales { get; set; }
        public decimal LastMonthSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal ThisMonthPurchases { get; set; }
        public decimal LastMonthPurchases { get; set; }
        public decimal ThisMonthDiscount { get; set; }
        public decimal LastMonthDiscount { get; set; }
        public decimal ThisMonthProfit { get; set; }
        public decimal LastMonthProfit { get; set; }
        public decimal ThisMonthSalesQty { get; set; }
        public decimal LastMonthSalesQty { get; set; }
        public decimal ThisMonthAvgCustomerSales { get; set; }
        public decimal LastMonthAvgCustomerSales { get; set; }
        public decimal ThisMonthAvgUnitsPerCustomer { get; set; }
        public decimal LastMonthAvgUnitsPerCustomer { get; set; }
        public decimal ThisMonthSalesReturn { get; set; }
        public decimal LastMonthSalesReturn { get; set; }
        public decimal TotalStockValue { get; set; }
        public int BelowMinimumStockCount { get; set; }
        public int AboveMaximumStockCount { get; set; }
        public int DeadStockCount { get; set; }
        public int ShortExpiryStockCount { get; set; }
        public decimal TotalNearExpiryAmount { get; set; }
        public decimal TotalPayable { get; set; }
        public List<PayableDetailVM> PayableDetails { get; set; } = new();
        public decimal TotalReceiveable { get; set; }
        public List<ReceivableDetailVM> ReceivableDetails { get; set; } = new();
        public decimal TotalCash { get; set; }
        public decimal TotalBank { get; set; }
        public decimal WeeklyPDCAmt { get; set; }
        public int WeeklyPDCCount { get; set; }
        public List<PDCDetailVM> PDCDetails { get; set; } = new();
        public List<string> SalesLabels { get; set; } = new();
        public List<decimal> SalesTotals { get; set; } = new();

        // Purchase Data
        public List<string> PurchaseLabels { get; set; } = new();
        public List<decimal> PurchaseTotals { get; set; } = new();
        public List<string> PendingPayableLabels { get; set; } = new();
        public List<decimal> PendingPayableTotals { get; set; } = new();
        public monthlypurchase[] MonthlysalesPurchase { get; set; } = Array.Empty<monthlypurchase>();


        public List<TopItemVM> Top10Items { get; set; } = new();
        public List<CategorySalesVM> CategoryWiseSales { get; set; } = new();
        public List<CompanySalesVM> CompanyWiseSales { get; set; } = new();
        public List<SalesProfitVM> MonthlySalesProfit { get; set; } = new();
        public List<PaymentModeVM> PaymentModeWiseCollection { get; set; } = new();
        public List<CategoryStockSalesVM> CategoryStockVsSales { get; set; } = new();
        public List<DeadStockDetailVM> DeadStockDetails { get; set; } = new();
        public List<ShortExpiryDetailVM> ShortExpiryDetails { get; set; } = new();
        public List<AboveMaxStockDetailVM> AboveMaxStockDetails { get; set; } = new();
        public List<BelowMinStockDetailVM> BelowMinStockDetails { get; set; } = new();
        public List<StockValueDetailVM> StockValueDetails { get; set; } = new();
        public decimal TotalStockValueAmount { get; set; }
        public List<CashInHandDetailVM> CashInHandDetails { get; set; } = new();
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public List<BankTransactionDetailVM> BankDetails { get; set; } = new();
        public decimal TotalBankIn { get; set; }
        public decimal TotalBankOut { get; set; }

    }
    public class monthlypurchase
    {
        public string? Label { get; set; }
        public decimal SalesTotal { get; set; }
        public decimal PurchaseTotal { get; set; }
    }

    public class ReceivableDetailVM
    {
        public string? Name { get; set; }
        public string? PhoneNo { get; set; }
        public string? BillNo { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public int DaysPending { get; set; }
    }

    public class PayableDetailVM
    {
        public string? SupplierName { get; set; }
        public string? PhoneNo { get; set; }
        public string? BillNo { get; set; }
        public DateTime BillDate { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public int DaysPending { get; set; }
    }

    public class PDCDetailVM
    {
        public string? SupplierName { get; set; }
        public string? PhoneNo { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime ChequeDate { get; set; }
        public string? BankName { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopItemVM
    {
        public string? ItemName { get; set; }
        public decimal TotalQty { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CategorySalesVM
    {
        public string? CategoryName { get; set; }
        public double TotalAmount { get; set; }
    }

    public class CompanySalesVM
    {
        public string? CompanyName { get; set; }
        public double TotalAmount { get; set; }
    }

    public class SalesProfitVM
    {
        public string? MonthName { get; set; }
        public double Sales { get; set; }
        public double Profit { get; set; }
        public double ProfitPct { get; set; }
    }

    public class PaymentModeVM
    {
        public string? ModeName { get; set; }
        public double TotalAmount { get; set; }
    }

    public class CategoryStockSalesVM
    {
        public string? CategoryName { get; set; }
        public double StockValue { get; set; }
        public double SalesValue { get; set; }
    }

    public class DeadStockDetailVM
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal StockValue { get; set; }
        public DateTime? LastSoldDate { get; set; }
        public int DaysNotSold { get; set; }
    }

    public class ShortExpiryDetailVM
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public string? Batch { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal StockValue { get; set; }
    }

    public class AboveMaxStockDetailVM
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MaximumQty { get; set; }
        public decimal ExcessQty { get; set; }
        public decimal StockValue { get; set; }
        public decimal ExcessValue { get; set; }
    }

    public class BelowMinStockDetailVM
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumQty { get; set; }
        public decimal ShortageQty { get; set; }
        public decimal StockValue { get; set; }
        public decimal ShortageValue { get; set; }
    }

    public class StockValueDetailVM
    {
        public string? ItemName { get; set; }
        public string? Category { get; set; }
        public string? Company { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal CostRate { get; set; }
        public decimal StockValue { get; set; }
    }

    public class CashInHandDetailVM
    {
        public string? Type { get; set; }        // "Sales Receipt" / "Payment"
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public decimal CashIn { get; set; }
        public decimal CashOut { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public class BankTransactionDetailVM
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public decimal BankIn { get; set; }
        public decimal BankOut { get; set; }
        public decimal RunningBalance { get; set; }
        public string? PaymentMode { get; set; }  // UPI / Cheque / NEFT etc
    }
}
