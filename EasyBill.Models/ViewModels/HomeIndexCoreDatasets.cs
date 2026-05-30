using AOne.Models.Entity;
using EasyBill.Models.Entity;

namespace EasyBill.Models.ViewModels
{
    public sealed class HomeIndexCoreDatasets
    {
        public IList<Sales> Sales { get; init; } = new List<Sales>();
        public IList<Purchase> Purchase { get; init; } = new List<Purchase>();
        public IList<PurchaseChallan> PurchaseChallans { get; init; } = new List<PurchaseChallan>();
        public IList<PaymentVoucher> Payments { get; init; } = new List<PaymentVoucher>();
        public IList<PurchaseReturn> PurchaseReturns { get; init; } = new List<PurchaseReturn>();
        public IList<StockIssue> StockIssues { get; init; } = new List<StockIssue>();
        public IList<StockReturn> StockReturns { get; init; } = new List<StockReturn>();
        public IList<StockReturn> StockReturnsAll { get; init; } = new List<StockReturn>();
        public IList<StockReceive> StockReceives { get; init; } = new List<StockReceive>();
        public IList<ItemMaster> AllItems { get; init; } = new List<ItemMaster>();
        public IList<CategoryMaster> Categories { get; init; } = new List<CategoryMaster>();
        public IList<Company> Companies { get; init; } = new List<Company>();
    }
}
