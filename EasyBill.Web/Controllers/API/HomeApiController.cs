using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]  
    public class HomeApiController : ControllerBase
    {
        private readonly ILogger<HomeApiController> _logger;
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly IPurchaseReturnRepository _purchaseReturnRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        private readonly IStockIssueRepository _stockIssueRepo;
        private readonly IStockReceiveRepository _stockReceiveRepo;
        private readonly IStockReturnRepository _stockReturnRepo;
        private readonly IpaymentVoucherRepository _paymentVoucherRepo;

        public HomeApiController(
            ILogger<HomeApiController> logger,
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo,
            IPurchaseReturnRepository purchaseReturnRepo,
            IItemMasterRepository itemMasterRepo,
            IStockIssueRepository stockIssueRepo,
            IStockReceiveRepository stockReceiveRepo,
            IStockReturnRepository stockReturnRepo,
            IpaymentVoucherRepository paymentVoucherRepo)
        {
            _logger = logger;
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
            _purchaseReturnRepo = purchaseReturnRepo;
            _itemMasterRepo = itemMasterRepo;
            _stockIssueRepo = stockIssueRepo;
            _stockReceiveRepo = stockReceiveRepo;
            _stockReturnRepo = stockReturnRepo;
            _paymentVoucherRepo = paymentVoucherRepo;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] string? tenantId)
        {
            if (!string.IsNullOrEmpty(tenantId))
            {
                var currentUserTenantId = User.FindFirst("TenantId")?.Value;
                var isSuperAdmin = User.IsInRole("SuperAdmin");

                if (!isSuperAdmin && tenantId != currentUserTenantId)
                {
                    return StatusCode(403, new { success = false, message = "Access denied: You cannot view data for another tenant." });
                }

                HttpContext.Items["OverrideTenantId"] = tenantId;
            }

            var salesData = await _salesRepo.GetAll();
            var purchaseData = await _purchaseRepo.GetAll();
            var payments = await _paymentVoucherRepo.GetAll();
            var purchaseReturns = await _purchaseReturnRepo.GetAll();
            var stockIssues = await _stockIssueRepo.GetAll();
            var stockReturns = await _stockReturnRepo.GetAll();
            var stockReceives = await _stockReceiveRepo.GetAll();

            var nearExpiryItems = await GetNearExpiryStockList(purchaseData, purchaseReturns, salesData, stockIssues, stockReturns, stockReceives);

            // Sales & Purchase Totals
            var totalSales = salesData.Sum(x => x.TotalPayable);
            var totalPurchases = purchaseData.Sum(x => x.TotalPayable);
            var totalNearExpiryAmount = nearExpiryItems.Sum(x => x.Stocks);

            // Flatten sales payment details
            var totalSalesPayments = salesData.SelectMany(x => x.SalsePaymentDetails);

            // Sum of Sales Payments: Cash & Bank
            var salesCash = totalSalesPayments.Where(x => x.ModeOfPayment?.Name == "Cash").Sum(x => x.Amount);
            var salesBank = totalSalesPayments.Where(x => x.ModeOfPayment?.Name != "Cash").Sum(x => x.Amount);

            // Sum of Payment Vouchers: Cash & Bank
            var paymentCash = payments.Where(x => x.paymentVouchercategory?.Name == "Cash").Sum(x => x.Amount);
            var paymentBank = payments.Where(x => x.paymentVouchercategory?.Name != "Cash").Sum(x => x.Amount);

            // Final Calculated Balances
            var totalCash = salesCash - paymentCash;
            var totalBank = salesBank - paymentBank;

            // Get this week’s cheque data
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-1 * diff).Date;
            var weekEnd = weekStart.AddDays(6).Date;

            var weeklyChequeData = payments
                                   .Where(p =>
                                       p.ChequeDate.HasValue &&
                                       p.ChequeDate.Value.Date >= weekStart &&
                                       p.ChequeDate.Value.Date <= weekEnd &&
                                       p.paymentVouchercategory?.Name?.ToLower() == "cheque")
                                   .ToList();

            var weeklyChequeAmount = weeklyChequeData.Sum(p => p.Amount);
            var weeklyChequeCount = weeklyChequeData.Count;

            var monthlySales = GroupMonthlyData(salesData.Select(x => (x.BillDate, x.TotalPayable)));
            var monthlyPurchase = GroupMonthlyData(purchaseData.Select(x => (x.BillDate, x.TotalPayable)));

            // Step 1: Group purchases by (Year, Month, Supplier)
            var purchaseGrouped = purchaseData
                .Where(p => p.BillDate.HasValue && p.SupplierId.HasValue)
                .GroupBy(p => (Year: p.BillDate.Value.Year, Month: p.BillDate.Value.Month, SupplierId: p.SupplierId.Value))
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.SupplierId,
                    TotalPurchase = g.Sum(p => p.TotalPayable)
                })
                .ToList();

            // Step 2: Group payments
            var paymentGroupedDict = payments
                .Where(p => p.Date.HasValue)
                .Select(p => new
                {
                    Year = p.Date.Value.Year,
                    Month = p.Date.Value.Month,
                    SupplierId = p.SupplierId,
                    Amount = p.Amount
                })
                .GroupBy(p => (p.Year, p.Month, p.SupplierId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Amount)
                );

            // Step 3: Join purchase & payment
            var pendingPayables = purchaseGrouped
                .Select(p =>
                {
                    var key = (p.Year, p.Month, p.SupplierId);
                    var totalPaid = paymentGroupedDict.TryGetValue(key, out var paidAmount) ? paidAmount : 0;

                    return new
                    {
                        p.Year,
                        p.Month,
                        p.SupplierId,
                        p.TotalPurchase,
                        TotalPaid = totalPaid,
                        PendingAmount = p.TotalPurchase - totalPaid
                    };
                })
                .ToList();

            var monthlyPendingChart = pendingPayables
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new
                {
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalPending = g.Sum(x => x.PendingAmount)
                })
                .OrderBy(x => DateTime.ParseExact(x.MonthName, "MMM yyyy", CultureInfo.InvariantCulture))
                .ToList();

            var vm = new DashboardVM
            {
                TotalSales = totalSales,
                TotalPurchases = totalPurchases,
                TotalNearExpiryAmount = totalNearExpiryAmount,

                TotalCash = totalCash,
                TotalBank = totalBank,
                WeeklyPDCAmt = weeklyChequeAmount,
                WeeklyPDCCount = weeklyChequeCount,

                SalesLabels = monthlySales.Select(x => x.MonthName).ToList(),
                SalesTotals = monthlySales.Select(x => x.Total).ToList(),

                PurchaseLabels = monthlyPurchase.Select(x => x.MonthName).ToList(),
                PurchaseTotals = monthlyPurchase.Select(x => x.Total).ToList(),

                PendingPayableLabels = monthlyPendingChart.Select(x => x.MonthName).ToList(),
                PendingPayableTotals = monthlyPendingChart.Select(x => x.TotalPending).ToList(),
                TotalPayable = monthlyPendingChart.Sum(x => x.TotalPending)
            };

            return Ok(vm);
        }

        private List<(string MonthName, decimal Total)> GroupMonthlyData(IEnumerable<(DateTime? BillDate, decimal TotalPayable)> data)
        {
            return data
                .Where(x => x.BillDate.HasValue)
                .GroupBy(x => x.BillDate.Value.Month)
                .Select(g => (
                    CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                    g.Sum(x => x.TotalPayable)))
                .OrderBy(x => DateTime.ParseExact(x.Item1, "MMM", CultureInfo.CurrentCulture).Month)
                .ToList();
        }

        private async Task<List<NearExpiryVM>> GetNearExpiryStockList(
            IEnumerable<Purchase> purchases,
            IEnumerable<PurchaseReturn> purchasereturns,
            IEnumerable<Sales> sales,
            IEnumerable<StockIssue> stockIssue,
            IEnumerable<StockReturn> stockReturn,
            IEnumerable<StockReceive> stockReceive)
        {
            var today = DateTime.Today;
            var expiryToDate = today.AddDays(60);

            var itemMasters = (await _itemMasterRepo.GetAll()).ToDictionary(x => x.Id);

            var purchaseItems = purchases.SelectMany(p => p.PurchaseItems ?? []);
            var purchaseReturnItems = purchasereturns.SelectMany(r => r.PurchaseReturnItems ?? []);
            var salesItems = sales.SelectMany(s => s.SalesItems ?? []);
            var stockIssueItems = stockIssue.SelectMany(s => s.StockIssuesItems ?? []);
            var stockReturnItems = stockReturn.SelectMany(s => s.StockReturnItems ?? []);
            var stockReceiveItems = stockReceive.SelectMany(s => s.StockReceiveItems ?? []);

            var allBatches = purchaseItems
                .Select(x => new { x.ItemId, x.Batch, ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue })
                .Concat(purchaseReturnItems.Select(x => new { x.ItemId, x.Batch, ExpiryDate = x.ExpiryDate ?? DateTime.MaxValue }))
                .Concat(salesItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockIssueItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReturnItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Concat(stockReceiveItems.Select(x => new { ItemId = x.ItemMasterId, x.Batch, ExpiryDate = x.Expirydate ?? DateTime.MaxValue }))
                .Where(b => b.ExpiryDate >= today && b.ExpiryDate <= expiryToDate)
                .Distinct()
                .ToList();

            var result = new List<NearExpiryVM>();

            foreach (var batch in allBatches)
            {
                if (!itemMasters.TryGetValue(batch.ItemId, out var item)) continue;

                decimal GetQty<T>(IEnumerable<T> items, Func<T, bool> filter, Func<T, decimal> selector) =>
                    items.Where(filter).Sum(selector);

                var purchasedQty = GetQty(purchaseItems, x => x.ItemId == batch.ItemId && x.Batch == batch.Batch && x.ExpiryDate == batch.ExpiryDate, x => x.Qty + x.FreeQty);
                var purchaseReturnQty = GetQty(purchaseReturnItems, x => x.ItemId == batch.ItemId && x.Batch == batch.Batch && x.ExpiryDate == batch.ExpiryDate, x => x.Qty + x.FreeQty);
                var salesQty = GetQty(salesItems, x => x.ItemMasterId == batch.ItemId && x.Batch == batch.Batch && x.Expirydate == batch.ExpiryDate, x => x.Qty);
                var stockIssueQty = GetQty(stockIssueItems, x => x.ItemMasterId == batch.ItemId && x.Batch == batch.Batch && x.Expirydate == batch.ExpiryDate, x => x.Qty);
                var stockReturnQty = GetQty(stockReturnItems, x => x.ItemMasterId == batch.ItemId && x.Batch == batch.Batch && x.Expirydate == batch.ExpiryDate, x => x.Qty);
                var stockReceiveQty = GetQty(stockReceiveItems, x => x.ItemMasterId == batch.ItemId && x.Batch == batch.Batch && x.Expirydate == batch.ExpiryDate, x => x.Qty);

                var currentStock = purchasedQty + stockReturnQty + stockReceiveQty - purchaseReturnQty - salesQty - stockIssueQty;
                if (currentStock <= 0) continue;

                var rate = purchaseItems
                    .FirstOrDefault(x => x.ItemId == batch.ItemId && x.Batch == batch.Batch && x.ExpiryDate == batch.ExpiryDate)?.Rate ?? 0;

                result.Add(new NearExpiryVM
                {
                    Stocks = Math.Round(currentStock * rate, 2),
                    ExpiryDate = batch.ExpiryDate
                });
            }

            return result.OrderBy(x => x.ExpiryDate).ToList();
        }
    }
}
