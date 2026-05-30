using AOne.Models.Entity;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ReportingServices.Interfaces;
using System.Globalization;

namespace AOne.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly IPurchaseReturnRepository _purchaseReturnRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        private readonly IStockIssueRepository _stockIssueRepo;
        private readonly IStockReceiveRepository _stockReceiveRepo;
        private readonly IStockReturnRepository _stockReturnRepo;
        private readonly IpaymentVoucherRepository _paymentVoucherRepo;
        private readonly ICategoryMasterRepository _categoryMasterRepo;
        private readonly ICompanyRepository _companyRepo;
        private readonly IPurchaseChallanRepository _purchaseChallanRepo;
        private readonly ISalseSettingRepository _salsesettingservice;
        private readonly IHomeIndexDashboardDataRepository _homeIndexDashboardDataRepository;

        public HomeController(
            ILogger<HomeController> logger,
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo,
            IPurchaseReturnRepository purchaseReturnRepo,
            IItemMasterRepository itemMasterRepo,
            IStockIssueRepository stockIssueRepo,
            IStockReceiveRepository stockReceiveRepo,
            IStockReturnRepository stockReturnRepo,
            IpaymentVoucherRepository paymentVoucherRepo,
            ICategoryMasterRepository categoryMasterRepo,
            ICompanyRepository companyRepository,
            IPurchaseChallanRepository purchaseChallanRepo,
            ISalseSettingRepository salsesettingservice,
            IHomeIndexDashboardDataRepository homeIndexDashboardDataRepository)
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
            _categoryMasterRepo = categoryMasterRepo;
            _companyRepo = companyRepository;
            _purchaseChallanRepo = purchaseChallanRepo;
            _salsesettingservice = salsesettingservice;
            _homeIndexDashboardDataRepository = homeIndexDashboardDataRepository;
        }

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            // ================================================================
            // 📅 DATE RANGE SETUP
            // ================================================================
            DateTime startDate;
            DateTime endDate;

            if (fromDate.HasValue && toDate.HasValue)
            {
                startDate = fromDate.Value.Date;
                endDate = toDate.Value.Date;
            }
            else
            {
                var currentday = DateTime.Today;
                startDate = new DateTime(currentday.Year, currentday.Month, 1);
                endDate = startDate.AddMonths(1).AddDays(-1);
            }

            ViewBag.FromDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = endDate.ToString("yyyy-MM-dd");

            var today = DateTime.Today;

            // This Month & Last Month ranges (for cards/metrics)
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var setting = await _salsesettingservice.GetByUserId(userId);
            var isTabletWise = setting?.ItemConversion == "tabletwise";
            // ================================================================
            // 📦 DATA FETCH (All repos)
            // ================================================================
            var dashboardData = await _homeIndexDashboardDataRepository.LoadCoreDatasetsAsync(
                startDate,
                endDate,
                HttpContext.RequestAborted);

            var sales = dashboardData.Sales;
            var purchase = dashboardData.Purchase;
            var purchaseChallans = dashboardData.PurchaseChallans;
            var payments = dashboardData.Payments;
            var purchaseReturns = dashboardData.PurchaseReturns;
            var stockIssues = dashboardData.StockIssues;
            var stockReturns = dashboardData.StockReturns;
            var stockReceives = dashboardData.StockReceives;
            var allItems = dashboardData.AllItems;

            // Filter by selected date range
            var salesData = sales.Where(x => x.BillDate >= startDate && x.BillDate <= endDate).ToList();
            var purchaseData = purchase.Where(x => x.BillDate >= startDate && x.BillDate <= endDate).ToList();


            // ================================================================
            // 🗂️ FLATTEN ITEMS (reusable across all calculations)
            // ================================================================
            var purchaseItems = purchase.SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>());
            var challanItems = purchaseChallans
                .Where(x => !x.ConvertedPurchaseId.HasValue && !string.Equals(x.Status, "Converted", StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x.PurchaseChallanItems ?? new List<PurchaseChallanItem>())
                .Select(x => new { x.ItemId, Qty = x.Qty + x.FreeQty });
            var purchaseReturnItems = purchaseReturns.SelectMany(x => x.PurchaseReturnItems ?? new List<PurchaseReturnItem>());
            var salesItems = sales.SelectMany(x => x.SalesItems ?? new List<SalesItem>());
            var stockIssueItems = stockIssues.SelectMany(x => x.StockIssuesItems ?? new List<StockIssueItem>());
            var stockReturnItems = stockReturns.SelectMany(x => x.StockReturnItems ?? new List<StockReturnItem>());
            var stockReceiveItems = stockReceives.SelectMany(x => x.StockReceiveItems ?? new List<StockReceiveItem>());


            // ================================================================
            // 📊 STOCK DICTIONARIES (item-wise totals for fast lookup)
            // ================================================================
            var purchaseDict = purchaseItems
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty + x.FreeQty));

            foreach (var challanGroup in challanItems.GroupBy(x => x.ItemId))
            {
                purchaseDict[challanGroup.Key] = purchaseDict.GetValueOrDefault(challanGroup.Key) + challanGroup.Sum(x => x.Qty);
            }

            var purchaseReturnDict = purchaseReturnItems
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty + x.FreeQty));

            var salesDict = salesItems
                .GroupBy(x => x.ItemMasterId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var issueDict = stockIssueItems
                .GroupBy(x => x.ItemMasterId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var returnDict = stockReturnItems
                .GroupBy(x => x.ItemMasterId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            var receiveDict = stockReceiveItems
                .GroupBy(x => x.ItemMasterId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));

            // Helper: Calculate current stock for any item
            decimal GetCurrentStock(int itemId)
            {
                decimal purchased = purchaseDict.GetValueOrDefault(itemId);
                decimal purchaseRet = purchaseReturnDict.GetValueOrDefault(itemId);
                decimal sold = salesDict.GetValueOrDefault(itemId);
                decimal issued = issueDict.GetValueOrDefault(itemId);
                decimal returned = returnDict.GetValueOrDefault(itemId);
                decimal received = receiveDict.GetValueOrDefault(itemId);
                return purchased + returned + received - purchaseRet - sold - issued;
            }


            // ================================================================
            // 💰 SALES & PURCHASE TOTALS (selected date range)
            // ================================================================
            var totalSales = salesData.Sum(x => x.TotalPayable);
            var totalPurchases = purchaseData.Sum(x => x.TotalPayable);

            // ================================================================
            // 💵 CASH & BANK (All Time — date filter independent)
            // ================================================================

            var salesCash = GetPaymentAmount(
                sales.Where(s => s.Deleted == null),
                s => s.SalsePaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                isCash: true);

            var salesBank = GetPaymentAmount(
                sales.Where(s => s.Deleted == null),
                s => s.SalsePaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                isCash: false);

            var purchaseCash = GetPaymentAmount(
                purchase.Where(p => p.Deleted == null),
                p => p.PaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                isCash: true);

            var purchaseBank = GetPaymentAmount(
                purchase.Where(p => p.Deleted == null),
                p => p.PaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                isCash: false);

            //var purchaseReturnCash = GetAmount(purchaseReturns, pr => pr.Totaldiscount ?? new List<PurchaseReturnPaymentDetails>(), true);
            //var purchaseReturnBank = GetAmount(purchaseReturns,  pr => pr.PurchaseReturnPaymentDetails ?? new List<PurchaseReturnPaymentDetails>(), false);

            //var salesReturnCash = GetAmount(stockReturns, sr => sr.PaymentDetails ?? new List<StockReturnPaymentDetails>(), true);
            //var salesReturnBank = GetAmount(stockReturns, sr => sr.PaymentDetails ?? new List<StockReturnPaymentDetails>(), false);

            // Payment Voucher se ALL TIME cash/bank gaya
            var voucherCash = payments
    .Where(p => p.ModeOfPayment != null &&
        string.Equals(p.ModeOfPayment.Name, "cash", StringComparison.OrdinalIgnoreCase)
        && p.Deleted == null)
    .Sum(p => p.Amount);

            var voucherBank = payments
                .Where(p => p.ModeOfPayment != null &&
                    !string.Equals(p.ModeOfPayment.Name, "cash", StringComparison.OrdinalIgnoreCase)
                    && p.Deleted == null)
                .Sum(p => p.Amount);

            //var paymentCash = payments.Where(x => x.paymentVouchercategory?.Name.ToLower() == "cash" && x.Deleted == null).Sum(x => x.Amount);

            //var paymentBank = payments.Where(x => x.paymentVouchercategory?.Name.ToLower() != "cash"  && x.Deleted == null).Sum(x => x.Amount);

            var totalCash = salesCash 
                //+ salesReturnCash
                - purchaseCash  
               // - purchaseReturnCash 
                - voucherCash;

            var totalBank = salesBank 
             //   + salesReturnBank
                - purchaseBank
    //- purchaseReturnBank
    - voucherBank;

            //var totalCash = salesCash - paymentCash;
            //var totalBank = salesBank - paymentBank;


            // ================================================================
            // 🏆 TOP 10 ITEMS BY SALES
            // ================================================================

            // Pehle ItemMaster dictionary banao
            var itemNameDict = allItems
                .ToDictionary(x => x.Id, x => x.Name ?? "Unknown");
            var itemMasterDict = allItems.ToDictionary(x => x.Id, x => x);



            var top10Items = sales
                  .Where(s => s.Deleted == null)
                  .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
                  .Where(x => x.si.Deleted == null)
                  .GroupBy(x => x.si.ItemMasterId)
                  .Select(g => new TopItemVM
                  {
                      ItemName = itemNameDict.GetValueOrDefault(g.Key, "Item #" + g.Key),
                      TotalQty = g.Sum(x => CalculateSalesQuantity(x.si, isTabletWise)),
                      TotalAmount = g.Sum(x => CalculateFinalSalesAmount(x.s, x.si, isTabletWise))
                  })
                  .OrderByDescending(x => x.TotalAmount)
                  .Take(10)
                  .ToList();



            // ================================================================
            // 📅 THIS MONTH vs LAST MONTH METRICS
            // ================================================================

            // --- Sales ---
            var thisMonthSales = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= thisMonthStart
                         && x.BillDate.Value.Date <= thisMonthEnd
                         && x.Deleted == null)
                .Sum(x => x.TotalPayable);

            var lastMonthSales = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= lastMonthStart
                         && x.BillDate.Value.Date <= lastMonthEnd
                         && x.Deleted == null)
                .Sum(x => x.TotalPayable);

            // --- Purchases ---
            var thisMonthPurchases = purchase
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= thisMonthStart
                         && x.BillDate.Value.Date <= thisMonthEnd)
                .Sum(x => x.TotalPayable);

            var lastMonthPurchases = purchase
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= lastMonthStart
                         && x.BillDate.Value.Date <= lastMonthEnd)
                .Sum(x => x.TotalPayable);

            // --- Discounts ---
            var thisMonthDiscount = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= thisMonthStart
                         && x.BillDate.Value.Date <= thisMonthEnd
                         && x.Deleted == null)
                .Sum(x => x.Totaldiscount);

            var lastMonthDiscount = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= lastMonthStart
                         && x.BillDate.Value.Date <= lastMonthEnd
                         && x.Deleted == null)
                .Sum(x => x.Totaldiscount);

            // --- Sales Qty ---
            var thisMonthSalesQty = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= thisMonthStart
                         && x.BillDate.Value.Date <= thisMonthEnd
                         && x.Deleted == null)
                .SelectMany(x => x.SalesItems).Sum(x => x.Qty);

            var lastMonthSalesQty = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= lastMonthStart
                         && x.BillDate.Value.Date <= lastMonthEnd
                         && x.Deleted == null)
                .SelectMany(x => x.SalesItems).Sum(x => x.Qty);


            // ================================================================
            // 👥 CUSTOMER METRICS (Avg Sales & Avg Units per Customer)
            // ================================================================
            var thisMonthCustomerCount = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= thisMonthStart
                         && x.BillDate.Value.Date <= thisMonthEnd
                         && x.Deleted == null)
                .Select(x => x.CustomerId).Distinct().Count();

            var lastMonthCustomerCount = sales
                .Where(x => x.BillDate.HasValue
                         && x.BillDate.Value.Date >= lastMonthStart
                         && x.BillDate.Value.Date <= lastMonthEnd
                         && x.Deleted == null)
                .Select(x => x.CustomerId).Distinct().Count();

            // Avg Sales per Customer
            var thisMonthAvgCustomerSales = thisMonthCustomerCount > 0
                ? Math.Round(thisMonthSales / thisMonthCustomerCount, 2) : 0;

            var lastMonthAvgCustomerSales = lastMonthCustomerCount > 0
                ? Math.Round(lastMonthSales / lastMonthCustomerCount, 2) : 0;

            // Avg Units per Customer
            var thisMonthAvgUnitsPerCustomer = thisMonthCustomerCount > 0
                ? Math.Round(thisMonthSalesQty / thisMonthCustomerCount, 1) : 0;

            var lastMonthAvgUnitsPerCustomer = lastMonthCustomerCount > 0
                ? Math.Round(lastMonthSalesQty / lastMonthCustomerCount, 1) : 0;


            // ================================================================
            // 🔄 SALES RETURNS (Stock Return repo se)
            // ================================================================
            var salesReturns = dashboardData.StockReturnsAll;

            var thisMonthSalesReturn = salesReturns
                .Where(x => x.ChallanDate.HasValue
                         && x.ChallanDate.Value.Date >= thisMonthStart
                         && x.ChallanDate.Value.Date <= thisMonthEnd)
                .Sum(x => x.TotalPayable);

            var lastMonthSalesReturn = salesReturns
                .Where(x => x.ChallanDate.HasValue
                         && x.ChallanDate.Value.Date >= lastMonthStart
                         && x.ChallanDate.Value.Date <= lastMonthEnd)
                .Sum(x => x.TotalPayable);


            
            var latestPurchaseCost = purchase
                       .Where(p => p.Deleted == null && p.BillDate.HasValue)
                       .SelectMany(p => p.PurchaseItems)
                       .Where(pi => pi.Deleted == null)
                       .GroupBy(pi => pi.ItemId)
                       .Select(g => new {
                           ItemId = g.Key,
                           CostRate = g
                               .OrderByDescending(x => x.Purchases?.BillDate) // ✅ FIX
                               .Select(x => x.BatchWiseCose)               // ✅ FIX
                               .FirstOrDefault()
                       })
                       .ToList(); 
            
             
            var latestPurchaseCostDict = latestPurchaseCost.ToDictionary(x => x.ItemId, x => x.CostRate);



            var thisMonthProfit = sales
                 .Where(s => s.BillDate.HasValue
                          && s.BillDate.Value.Date >= thisMonthStart
                          && s.BillDate.Value.Date <= thisMonthEnd
                          && s.Deleted == null)
                 .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
                 .Where(x => x.si.Deleted == null)
                 .Sum(x => CalculateSalesProfitAmount(x.s, x.si, isTabletWise, latestPurchaseCostDict));

             
            var lastMonthProfit = sales
                     .Where(s => s.BillDate.HasValue
                              && s.BillDate.Value.Date >= lastMonthStart
                              && s.BillDate.Value.Date <= lastMonthEnd
                              && s.Deleted == null)
                     .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
                     .Where(x => x.si.Deleted == null)
                     .Sum(x => CalculateSalesProfitAmount(x.s, x.si, isTabletWise, latestPurchaseCostDict));

            #region STOCK OVERVIEW
            // ================================================================
            // 📦 STOCK OVERVIEW METRICS
            // ================================================================

            // --- Total Stock Value ---
            var totalStockValue = latestPurchaseCost.Sum(item => {
                var currentStock = GetCurrentStock(item.ItemId);
                var stock = currentStock < 0 ? 0 : currentStock;
                return stock * item.CostRate;
            });

            // --- Below Minimum Stock Count ---
            var belowMinimumStockCount = allItems
                .Where(item => item.Deleted == null)
                .Count(item => {
                    decimal totalPurchased = purchaseDict.GetValueOrDefault(item.Id);
                    decimal currentStock = GetCurrentStock(item.Id);
                    return totalPurchased > 0 && currentStock < item.MinimumQty;
                });

            // --- Above Maximum Stock Count ---
            var aboveMaximumStockCount = allItems
                .Where(item => item.Deleted == null)
                .Count(item => {
                    decimal totalPurchased = purchaseDict.GetValueOrDefault(item.Id);
                    decimal currentStock = GetCurrentStock(item.Id);
                    return totalPurchased > 0
                        && item.MaximumQty > 0
                        && currentStock > item.MaximumQty;
                });

            // --- Dead Stock Count (not sold in last 90 days, purchased > 90 days ago) ---
            var ninetyDaysAgo = today.AddDays(-90);

            var soldItemIdsLast90Days = sales
                .Where(x => x.BillDate.HasValue && x.BillDate.Value.Date >= ninetyDaysAgo)
                .SelectMany(x => x.SalesItems ?? new List<SalesItem>())
                .Select(x => x.ItemMasterId)
                .Distinct()
                .ToHashSet();

            var firstPurchaseDatePerItem = purchase
                .Where(x => x.BillDate.HasValue && x.Deleted == null)
                .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>(),
                    (p, pi) => new { pi.ItemId, p.BillDate })
                .GroupBy(x => x.ItemId)
                .ToDictionary(g => g.Key, g => g.Min(x => x.BillDate));

            var deadStockCount = allItems
                .Where(item => item.Deleted == null)
                .Count(item => {
                    var itemId = item.Id;
                    decimal purchased = purchaseDict.GetValueOrDefault(itemId);
                    decimal currentStock = GetCurrentStock(itemId);
                    var firstPurchaseDate = firstPurchaseDatePerItem.GetValueOrDefault(itemId);

                    return purchased > 0
                        && currentStock > 0
                        && !soldItemIdsLast90Days.Contains(itemId)
                        && firstPurchaseDate.HasValue
                        && firstPurchaseDate.Value.Date <= ninetyDaysAgo;
                });


            // --- Short Expiry Stock Count (expiry within next 6 months, stock > 0) ---
            var sixMonthsLater = today.AddMonths(6);

            var expiryBatches = purchase
                .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>())
                .Where(x => x.ExpiryDate.HasValue
                         && x.ExpiryDate.Value >= today
                         && x.ExpiryDate.Value <= sixMonthsLater)
                .GroupBy(x => new { x.ItemId, x.Batch, x.ExpiryDate })
                .Select(g => g.Key.ItemId)
                .Distinct()
                .ToList();

            var shortExpiryCount = expiryBatches
                .Count(itemId => GetCurrentStock(itemId) > 0);
            #endregion

            #region Accounts Overview
            // ================================================================
            // 💚 RECEIVABLES DETAIL
            // ================================================================
            var receivableDetails = sales
                .Where(s => s.PaymentType == "credit"
                         && s.Balance > 0
                         && s.Deleted == null
                         && s.BillDate.HasValue)
                .Select(s => new ReceivableDetailVM
                {
                    Name = s.Customers?.Name ?? "",
                    PhoneNo = s.Customers?.PhoneNo ?? "",
                    BillNo = s.BillNo,
                    BillDate = s.BillDate.Value,
                    TotalPayable = s.TotalPayable,
                    PaidAmount = s.PaidAmount,
                    Balance = s.Balance,
                    DaysPending = (DateTime.Today - s.BillDate.Value.Date).Days
                })
                .OrderByDescending(x => x.DaysPending)
                .ToList();

            var totalReceiveable = receivableDetails.Sum(x => x.Balance);


            // ================================================================
            // 🔴 PAYABLES DETAIL
            // ================================================================

            // Supplier wise total paid (PaymentVoucher se)
            var supplierPaidDict = payments
                .Where(pv => pv.Deleted == null && pv.SupplierId.HasValue)
                .GroupBy(pv => pv.SupplierId.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // Detail list
            var payableDetails = purchase
                .Where(p => p.Deleted == null && p.SupplierId.HasValue)
                .Select(p => new PayableDetailVM
                {
                    SupplierName = p.Suppliers?.FirstName ?? "",
                    PhoneNo = p.Suppliers?.PhoneNO ?? "",
                    BillNo = p.BillNo,
                    BillDate = p.BillDate.Value,
                    TotalPayable = p.TotalPayable,
                    PaidAmount = supplierPaidDict.GetValueOrDefault(p.SupplierId.Value, 0),
                    Balance = p.TotalPayable - supplierPaidDict.GetValueOrDefault(p.SupplierId.Value, 0),
                    DaysPending = (DateTime.Today - p.BillDate.Value.Date).Days
                })
                .Where(p => p.Balance > 0 && p.BillDate != default)
                .OrderByDescending(x => x.DaysPending)
                .ToList();

            var totalPayable = payableDetails.Sum(x => x.Balance);

            #endregion

            // ================================================================
            // 🏷️ NEAR EXPIRY (existing helper method)
            // ================================================================
            var nearExpiryItems = await GetNearExpiryStockList(
                purchaseData, purchaseReturns, salesData, stockIssues, stockReturns, stockReceives);

            var totalNearExpiryAmount = nearExpiryItems.Sum(x => x.Stocks);


            // ================================================================
            // 💳 WEEKLY PDC / CHEQUE
            // ================================================================
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff).Date;
            var weekEnd = weekStart.AddDays(6).Date;

            //var weeklyChequeData = payments
            //    .Where(p => p.ChequeDate.HasValue
            //             && p.ChequeDate.Value.Date >= weekStart
            //             && p.ChequeDate.Value.Date <= weekEnd
            //             && p.paymentVouchercategory?.Name?.ToLower() == "cheque")
            //    .ToList();

            //var weeklyChequeAmount = weeklyChequeData.Sum(p => p.Amount);
            //var weeklyChequeCount = weeklyChequeData.Count;

            // PDC Detail with VM
            // ================================================================
            // 📅 PDC DETAIL
            // ================================================================
            var allPaymentVouchers = payments;

            var pdcDetails = allPaymentVouchers
                .Where(p => p.ChequeDate.HasValue
                         && p.ChequeDate.Value.Date >= weekStart
                         && p.ChequeDate.Value.Date <= weekEnd
                         && p.paymentVouchercategory?.Name?.ToLower() == "cheque"
                         && p.Deleted == null
                         && p.SupplierId.HasValue)
                .Select(p => new PDCDetailVM
                {
                    SupplierName = p.Suppliers?.FirstName ?? "",
                    PhoneNo = p.Suppliers?.PhoneNO ?? "",
                    ChequeNo = p.ChequeNo ?? "",
                    ChequeDate = p.ChequeDate.Value,
                    //BankName = p.BankName ?? "",
                    BankName = "",
                    Amount = p.Amount
                })
                .OrderBy(x => x.ChequeDate)
                .ToList();

            var weeklyChequeAmount = pdcDetails.Sum(p => p.Amount);
            var weeklyChequeCount = pdcDetails.Count;

            //var weeklyChequeAmount = pdcDetails.Sum(p => p.Amount);
            //var weeklyChequeCount = pdcDetails.Count;


            // ================================================================
            // 💸 PENDING PAYABLES (Purchase - Supplier Payments)
            // ================================================================
            //var totalPurchaseAmount = purchaseData.Sum(x => x.TotalPayable);

            //var supplierPayments = payments
            //    .Where(p => p.SupplierId.HasValue
            //             && p.Suppliers?.AccountGroup?.Name.ToLower() == "sundry creditor")
            //    .Sum(p => p.Amount);

            //var totalPayable = totalPurchaseAmount - supplierPayments;


            // Category dictionary banao
            // CategoryId Guid type hai, isliye Guid dictionary banao
            var categoryDict = dashboardData.Categories.ToDictionary(x => x.Id, x => x.CategoryName ?? "Unknown");
            // x.Id already Guid hoga — match ho jayega

            //var categoryWiseSales = sales
            //    .Where(s => s.Deleted == null)
            //    .SelectMany(s => s.SalesItems ?? new List<SalesItem>())
            //    .Where(si => si.ItemMaster != null && si.ItemMaster.CategoryId != null)
            //    .GroupBy(si => si.ItemMaster.CategoryId!.Value)   // ✅ .Value for nullable Guid
            //    .Select(g => new CategorySalesVM
            //    {
            //        CategoryName = categoryDict.ContainsKey(g.Key) ? categoryDict[g.Key] : "Unknown",
            //        TotalAmount = (double)g.Sum(x => x.Qty * x.Rate)
            //    })
            //    .OrderByDescending(x => x.TotalAmount)
            //    .ToList();

         var categoryWiseSales = sales
                  .Where(s => s.Deleted == null)
                  .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
                  .Where(x => x.si.Deleted == null)
                  .ToList()
                  .Select(x => {
                      var item = itemMasterDict.ContainsKey(x.si.ItemMasterId) 
                          ? itemMasterDict[x.si.ItemMasterId] 
                          : null;
                      var catId = item?.CategoryId;
                      var catName = catId.HasValue && categoryDict.ContainsKey(catId.Value) 
                          ? categoryDict[catId.Value] 
                          : "Uncategorized";
                      return new { x.s, x.si, catName };
                  })
                  .GroupBy(x => x.catName)
                  .Select(g => new CategorySalesVM
                  {
                      CategoryName = g.Key,
                      TotalAmount = (double)g.Sum(x => CalculateFinalSalesAmount(x.s, x.si, isTabletWise))
                  })
                  .OrderByDescending(x => x.TotalAmount)
                  .ToList();


            // Company dictionary
            var companyDict = dashboardData.Companies
                .ToDictionary(x => x.Id, x => x.Name ?? "Unknown");

            //var companyWiseSales = sales
            //    .Where(s => s.Deleted == null)
            //    .SelectMany(s => s.SalesItems ?? new List<SalesItem>())
            //    .Where(si => si.ItemMaster != null && si.ItemMaster.CompanyId != null)
            //    .GroupBy(si => si.ItemMaster.CompanyId.Value)
            //    .Select(g => new CompanySalesVM
            //    {
            //        CompanyName = companyDict.ContainsKey(g.Key) ? companyDict[g.Key] : "Unknown",
            //        TotalAmount = (double)g.Sum(x => x.Qty * x.Rate)
            //    })
            //    .OrderByDescending(x => x.TotalAmount)
            //    .ToList();

            var companyWiseSalesRaw = sales
             .Where(s => s.Deleted == null)
             .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
             .Where(x => x.si.ItemMaster != null
                      && x.si.ItemMaster.CompanyId != null
                      && x.si.Deleted == null)
             .GroupBy(x => x.si.ItemMaster.CompanyId!.Value)
             .Select(g => new
             {
                 CompanyName = companyDict.ContainsKey(g.Key) ? companyDict[g.Key] : "Unknown",
                 TotalAmount = g.Sum(x => CalculateFinalSalesAmount(x.s, x.si, isTabletWise))
             })
             .OrderByDescending(x => x.TotalAmount)
             .ToList();

            // ✅ Top 5
            var top5 = companyWiseSalesRaw.Take(5).ToList();

            // ✅ Others
            var others = companyWiseSalesRaw.Skip(5).ToList();

            if (others.Any())
            {
                top5.Add(new
                {
                    CompanyName = "Others",
                    TotalAmount = others.Sum(x => x.TotalAmount)
                });
            }

            // ✅ Final VM
            var companyWiseSales = top5.Select(x => new CompanySalesVM
            {
                CompanyName = x.CompanyName,
                TotalAmount = (double)x.TotalAmount
            }).ToList();

            // Monthly pending chart data
            var purchaseGrouped = purchaseData
                .Where(p => p.BillDate.HasValue && p.SupplierId.HasValue)
                .GroupBy(p => (Year: p.BillDate.Value.Year, Month: p.BillDate.Value.Month, SupplierId: p.SupplierId.Value))
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.SupplierId,
                    TotalPurchase = g.Sum(p => p.TotalPayable)
                }).ToList();

            var paymentGroupedDict = payments
                .Where(p => p.Date.HasValue)
                .GroupBy(p => (Year: p.Date.Value.Year, Month: p.Date.Value.Month, SupplierId: p.SupplierId))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var monthlyPendingChart = purchaseGrouped
                .Select(p => new {
                    p.Year,
                    p.Month,
                    PendingAmount = p.TotalPurchase - (paymentGroupedDict.TryGetValue(
                        (p.Year, p.Month, p.SupplierId), out var paid) ? paid : 0)
                })
                .GroupBy(x => new { x.Year, x.Month })
                .Select(g => new {
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    TotalPending = g.Sum(x => x.PendingAmount)
                })
                .OrderBy(x => DateTime.ParseExact(x.MonthName, "MMM yyyy", CultureInfo.InvariantCulture))
                .ToList();


            // ================================================================
            // 📊 CHART DATA (Monthly & Date-wise)
            // ================================================================
            var monthlySales = GroupMonthlyData(sales.Select(x => (x.BillDate, x.TotalPayable)));
            var monthlyPurchase = GroupMonthlyData(purchase.Select(x => (x.BillDate, x.TotalPayable)));

            var datewiseSales = salesData.GroupBy(x => x.BillDate.Value.Date)
                .Select(z => new { Date = z.Key, Amount = z.Sum(x => x.TotalPayable) }).ToList();

            var datewisePurchase = purchaseData.GroupBy(x => x.BillDate.Value.Date)
                .Select(z => new { Date = z.Key, Amount = z.Sum(x => x.TotalPayable) }).ToList();

            var allDates = datewiseSales.Select(x => x.Date)
                .Union(datewisePurchase.Select(x => x.Date))
                .OrderBy(x => x).ToList();

            var monthlySalesPurchaseList = allDates.Select(d => new monthlypurchase
            {
                Label = d.ToString("dd-MM-yyyy"),
                SalesTotal = datewiseSales.FirstOrDefault(x => x.Date == d)?.Amount ?? 0,
                PurchaseTotal = datewisePurchase.FirstOrDefault(x => x.Date == d)?.Amount ?? 0
            }).ToArray();


            // ================================================================
            // 📊 MONTHLY SALES vs PROFIT vs PROFIT %
            // ================================================================
            //var monthlySalesProfit = sales
            //    .Where(s => s.Deleted == null && s.BillDate.HasValue)
            //    .GroupBy(s => new { s.BillDate.Value.Year, s.BillDate.Value.Month })
            //    .Select(g => {
            //        var monthSales = (double)g.Sum(x => x.TotalPayable);
            //        var monthProfit = (double)(
            //            from si in g.SelectMany(s => s.SalesItems ?? new List<SalesItem>())
            //            join pc in latestPurchaseCost on si.ItemMasterId equals pc.ItemId
            //            select (si.Rate - pc.CostRate) * si.Qty
            //        ).Sum();
            //        return new SalesProfitVM
            //        {
            //            MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy"),
            //            Sales = monthSales,
            //            Profit = monthProfit,
            //            ProfitPct = monthSales > 0 ? Math.Round((monthProfit / monthSales) * 100, 1) : 0
            //        };
            //    })
            //    .OrderBy(x => DateTime.ParseExact(x.MonthName, "MMM yy", CultureInfo.InvariantCulture))
            //    .ToList();

            var monthlySalesProfit = sales
            .Where(s => s.Deleted == null && s.BillDate.HasValue && s.BillDate.Value.Date >= startDate && s.BillDate.Value.Date <= endDate).GroupBy(s => new { s.BillDate.Value.Year, s.BillDate.Value.Month })
            .Select(g =>
            {
                var monthlyItems = g
                    .SelectMany(sale => sale.SalesItems ?? new List<SalesItem>(), (sale, si) => new { sale, si })
                    .Where(x => x.si.Deleted == null)
                    .ToList();

                var monthSales = monthlyItems.Sum(x => CalculateFinalSalesAmount(x.sale, x.si, isTabletWise));
                var monthProfit = monthlyItems.Sum(x => CalculateSalesProfitAmount(x.sale, x.si, isTabletWise, latestPurchaseCostDict));

                return new SalesProfitVM
                {
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yy"),
                    Sales = (double)monthSales,
                    Profit = (double)monthProfit,
                    ProfitPct = monthSales > 0
                        ? Math.Round((double)(monthProfit / monthSales * 100), 1)
                        : 0
                };
            })
            .OrderBy(x => DateTime.ParseExact(x.MonthName, "MMM yy", CultureInfo.InvariantCulture))
            .ToList();

            // ================================================================
            // 💳 MODE OF PAYMENT WISE COLLECTION
            // ================================================================
            var paymentModeWiseCollection = sales
                .Where(s => s.Deleted == null)
                .Where(s => s.SalsePaymentDetails != null)
                .SelectMany(s => s.SalsePaymentDetails.ToList())
                .Where(spd => spd.ModeOfPayment != null)
                .GroupBy(spd => spd.ModeOfPayment.Name)
                .Select(g => new PaymentModeVM
                {
                    ModeName = g.Key ?? "Unknown",
                    TotalAmount = (double)g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            // ================================================================
            // 📦 STOCK VALUE CATEGORYWISE VS SALES
            // ================================================================
            //var categoryStockVsSales = (
            //    from im in allItems
            //    where im.Deleted == null && im.CategoryId != null
            //    let categoryName = categoryDict.ContainsKey(im.CategoryId!.Value)
            //                       ? categoryDict[im.CategoryId.Value] : "Unknown"
            //    let stockVal = latestPurchaseCost
            //                    .Where(x => x.ItemId == im.Id)
            //                    .Sum(x => GetCurrentStock(im.Id) < 0 ? 0 : GetCurrentStock(im.Id) * x.CostRate)
            //    let salesVal = sales
            //                    .Where(s => s.Deleted == null)
            //                    .SelectMany(s => s.SalesItems ?? new List<SalesItem>())
            //                    .Where(si => si.ItemMasterId == im.Id)
            //                    .Sum(si => si.Qty * si.Rate)
            //    group new { stockVal, salesVal } by categoryName into g
            //    select new CategoryStockSalesVM
            //    {
            //        CategoryName = g.Key,
            //        StockValue = (double)g.Sum(x => x.stockVal),
            //        SalesValue = (double)g.Sum(x => x.salesVal)
            //    }
            //).OrderByDescending(x => x.StockValue).ToList();

            var categoryStockVsSales = (
                from im in allItems
                where im.Deleted == null && im.CategoryId != null
            
                let categoryName = categoryDict.ContainsKey(im.CategoryId!.Value)
                                   ? categoryDict[im.CategoryId.Value]
                                   : "Unknown"
            
                let stockVal = latestPurchaseCost
                                .Where(x => x.ItemId == im.Id)
                                .Sum(x =>
                                {
                                    var stock = GetCurrentStock(im.Id);
                                    return stock < 0 ? 0 : stock * x.CostRate;
                                })
            
                let salesVal = sales
                    .Where(s => s.Deleted == null)
                    .SelectMany(s => s.SalesItems ?? new List<SalesItem>(), (s, si) => new { s, si })
                    .Where(x => x.si.ItemMasterId == im.Id && x.si.Deleted == null)
                    .Sum(x => CalculateFinalSalesAmount(x.s, x.si, isTabletWise))
            
                group new { stockVal, salesVal } by categoryName into g
            
                select new CategoryStockSalesVM
                {
                    CategoryName = g.Key,
                    StockValue = (double)g.Sum(x => x.stockVal),
                    SalesValue = (double)g.Sum(x => x.salesVal)
                }
            )
            .OrderByDescending(x => x.StockValue)
            .ToList();

            var deadStockDetails = allItems
                .Where(item => item.Deleted == null)
                .Where(item => {
                    var itemId = item.Id;
                    decimal purchased = purchaseDict.GetValueOrDefault(itemId);
                    decimal currentStock = GetCurrentStock(itemId);
                    var firstPurchaseDate = firstPurchaseDatePerItem.GetValueOrDefault(itemId);
                    return purchased > 0
                        && currentStock > 0
                        && !soldItemIdsLast90Days.Contains(itemId)
                        && firstPurchaseDate.HasValue
                        && firstPurchaseDate.Value.Date <= ninetyDaysAgo;
                })
                .Select(item => {
                    var currentStock = GetCurrentStock(item.Id);
                    var costRate = latestPurchaseCost
                        .FirstOrDefault(x => x.ItemId == item.Id)?.CostRate ?? 0;
                    var categoryName = item.CategoryId.HasValue && categoryDict.ContainsKey(item.CategoryId.Value)
                        ? categoryDict[item.CategoryId.Value] : "Unknown";
                    var lastSold = sales
                        .Where(s => s.Deleted == null)
                        .SelectMany(s => s.SalesItems ?? new List<SalesItem>())
                        .Where(si => si.ItemMasterId == item.Id)
                        .OrderByDescending(si => si.Id)
                        .FirstOrDefault();
                    return new DeadStockDetailVM
                    {
                        ItemName = item.Name ?? "Unknown",
                        Category = categoryName,
                        CurrentStock = currentStock,
                        StockValue = currentStock * costRate,
                        LastSoldDate = null, // if you have date on SalesItem use it
                        DaysNotSold = 90   // minimum 90 days
                    };
                })
                .OrderByDescending(x => x.StockValue)
                .ToList();

             var shortExpiryDetails = purchase
                 .SelectMany(x => x.PurchaseItems ?? new List<PurchaseItem>())
                 .Where(x => x.ExpiryDate.HasValue
                          && x.ExpiryDate.Value >= today
                          && x.ExpiryDate.Value <= sixMonthsLater)
                 .GroupBy(x => new { x.ItemId, x.Batch, x.ExpiryDate })
                 .Select(g => {
                     var itemId = g.Key.ItemId;
                     var currentStock = GetCurrentStock(itemId);
                     if (currentStock <= 0) return null;
             
                     var item = allItems.FirstOrDefault(i => i.Id == itemId);
                     var costRate = latestPurchaseCost
                         .FirstOrDefault(x => x.ItemId == itemId)?.CostRate ?? 0;
                     var categoryName = item?.CategoryId.HasValue == true && categoryDict.ContainsKey(item.CategoryId.Value)
                         ? categoryDict[item.CategoryId.Value] : "Unknown";
             
                     return new ShortExpiryDetailVM
                     {
                         ItemName = item?.Name ?? "Unknown",
                         Category = categoryName,
                         Batch = g.Key.Batch ?? "-",
                         ExpiryDate = g.Key.ExpiryDate!.Value,
                         DaysToExpiry = (g.Key.ExpiryDate.Value - today).Days,
                         CurrentStock = currentStock,
                         StockValue = currentStock * costRate
                     };
                 })
                 .Where(x => x != null)
                 .OrderBy(x => x.DaysToExpiry)
                 .ToList();

            var aboveMaxStockDetails = allItems
                .Where(item => item.Deleted == null)
                .Where(item => {
                    decimal purchased = purchaseDict.GetValueOrDefault(item.Id);
                    decimal currentStock = GetCurrentStock(item.Id);
                    return purchased > 0
                        && item.MaximumQty > 0
                        && currentStock > item.MaximumQty;
                })
                .Select(item => {
                    var currentStock = GetCurrentStock(item.Id);
                    var costRate = latestPurchaseCost
                        .FirstOrDefault(x => x.ItemId == item.Id)?.CostRate ?? 0;
                    var categoryName = item.CategoryId.HasValue && categoryDict.ContainsKey(item.CategoryId.Value)
                        ? categoryDict[item.CategoryId.Value] : "Unknown";
                    var excessQty = currentStock - item.MaximumQty;

                    return new AboveMaxStockDetailVM
                    {
                        ItemName = item.Name ?? "Unknown",
                        Category = categoryName,
                        CurrentStock = currentStock,
                        MaximumQty = item.MaximumQty,
                        ExcessQty = excessQty,
                        StockValue = currentStock * costRate,
                        ExcessValue = excessQty * costRate
                    };
                })
                .OrderByDescending(x => x.ExcessQty)
                .ToList();


            var belowMinStockDetails = allItems
                .Where(item => item.Deleted == null)
                .Where(item => {
                    decimal purchased = purchaseDict.GetValueOrDefault(item.Id);
                    decimal currentStock = GetCurrentStock(item.Id);
                    return purchased > 0 && currentStock < item.MinimumQty;
                })
                .Select(item => {
                    var currentStock = GetCurrentStock(item.Id);
                    var costRate = latestPurchaseCost
                        .FirstOrDefault(x => x.ItemId == item.Id)?.CostRate ?? 0;
                    var categoryName = item.CategoryId.HasValue && categoryDict.ContainsKey(item.CategoryId.Value)
                        ? categoryDict[item.CategoryId.Value] : "Unknown";
                    var shortageQty = item.MinimumQty - currentStock;

                    return new BelowMinStockDetailVM
                    {
                        ItemName = item.Name ?? "Unknown",
                        Category = categoryName,
                        CurrentStock = currentStock < 0 ? 0 : currentStock,
                        MinimumQty = item.MinimumQty,
                        ShortageQty = shortageQty,
                        StockValue = currentStock * costRate,
                        ShortageValue = shortageQty * costRate
                    };
                })
                .OrderByDescending(x => x.ShortageQty)
                .ToList();


            var stockValueDetails = allItems
                .Where(item => item.Deleted == null)
                .Select(item => {
                    var currentStock = GetCurrentStock(item.Id);
                    if (currentStock <= 0) return null;

                    var costRate = latestPurchaseCost
                        .FirstOrDefault(x => x.ItemId == item.Id)?.CostRate ?? 0;
                    if (costRate == 0) return null;

                    var categoryName = item.CategoryId.HasValue && categoryDict.ContainsKey(item.CategoryId.Value)
                        ? categoryDict[item.CategoryId.Value] : "Unknown";

                    var companyName = item.CompanyId.HasValue && companyDict.ContainsKey(item.CompanyId.Value)
                        ? companyDict[item.CompanyId.Value] : "Unknown";

                    return new StockValueDetailVM
                    {
                        ItemName = item.Name ?? "Unknown",
                        Category = categoryName,
                        Company = companyName,
                        CurrentStock = currentStock,
                        CostRate = costRate,
                        StockValue = currentStock * costRate
                    };
                })
                .Where(x => x != null)
                .OrderByDescending(x => x.StockValue)
                .ToList();


            // Cash IN — Sales se
            var cashInEntries = sales
                .Where(s => s.Deleted == null)
                .SelectMany(
                    s => s.SalsePaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                    (s, spd) => new { s, spd }
                )
                .Where(x =>
                    string.Equals(x.spd.ModeOfPayment?.Name, "cash", StringComparison.OrdinalIgnoreCase)
                    && x.s.BillDate.HasValue
                )
                .Select(x => new CashInHandDetailVM
                {
                    Type = "Sales Receipt",
                    Description = "Bill #" + x.s.BillNo +
                        (x.s.Customers?.Name != null ? " — " + x.s.Customers.Name : ""),
                    Date = x.s.BillDate.Value,
                    CashIn = x.spd.Amount,
                    CashOut = 0
                })
                .ToList();

            // Cash OUT — Payment Voucher se
            var cashOutEntries = payments
                .Where(p => p.ModeOfPayment != null
                         && string.Equals(p.ModeOfPayment.Name, "cash", StringComparison.OrdinalIgnoreCase)
                         && p.Deleted == null
                         && p.Date.HasValue)
                .Select(p => new CashInHandDetailVM
                {
                    Type = "Payment",
                    Description = "Voucher #" + (p.VouncherNo ?? "-") +
                                  (p.Suppliers?.FirstName != null ? " — " + p.Suppliers.FirstName : ""),
                    Date = p.Date.Value,
                    CashIn = 0,
                    CashOut = p.Amount
                }).ToList();

            // Combine, sort, running balance calculate karo
            var allCashEntries = cashInEntries
                .Concat(cashOutEntries)
                .OrderBy(x => x.Date)
                .ToList();

            decimal runningBal = 0;
            foreach (var entry in allCashEntries)
            {
                runningBal += entry.CashIn - entry.CashOut;
                entry.RunningBalance = runningBal;
            }

            // Reverse for display (latest first)
            allCashEntries = allCashEntries.OrderByDescending(x => x.Date).ToList();

            var totalCashIn = allCashEntries.Sum(x => x.CashIn);
            var totalCashOut = allCashEntries.Sum(x => x.CashOut);



            // Bank IN — Sales se (non-cash payments)
            var bankInEntries = sales
                .Where(s => s.Deleted == null)
                .SelectMany(
                    s => s.SalsePaymentDetails ?? Enumerable.Empty<SalsePaymentDetails>(),
                    (s, spd) => new { s, spd }
                )
                .Where(x =>
                    x.spd.ModeOfPayment != null &&
                    !string.Equals(x.spd.ModeOfPayment.Name, "cash", StringComparison.OrdinalIgnoreCase)
                    && x.s.BillDate.HasValue
                )
                .Select(x => new BankTransactionDetailVM
                {
                    Type = "Sales Receipt",
                    Description = "Bill #" + x.s.BillNo +
                                  (x.s.Customers?.Name != null ? " — " + x.s.Customers.Name : ""),
                    Date = x.s.BillDate.Value,
                    BankIn = x.spd.Amount,
                    BankOut = 0,
                    PaymentMode = x.spd.ModeOfPayment.Name
                })
                .ToList();

            // Bank OUT — Payment Voucher se (non-cash / PaymentType != 1)
            var bankOutEntries = payments
                .Where(p => p.ModeOfPayment != null
                         && !string.Equals(p.ModeOfPayment.Name, "cash", StringComparison.OrdinalIgnoreCase)
                         && p.Deleted == null
                         && p.Date.HasValue)
                .Select(p => new BankTransactionDetailVM
                {
                    Type = "Payment",
                    Description = "Voucher #" + (p.VouncherNo ?? "-") +
                                  (p.Suppliers?.FirstName != null ? " — " + p.Suppliers.FirstName : ""),
                    Date = p.Date.Value,
                    BankIn = 0,
                    BankOut = p.Amount,
                    PaymentMode = p.ModeOfPayment.Name
                })
                .ToList();

            // Combine, sort, running balance
            var allBankEntries = bankInEntries.Concat(bankOutEntries).OrderBy(x => x.Date).ToList();

            decimal bankRunningBal = 0;
            foreach (var entry in allBankEntries)
            {
                bankRunningBal += entry.BankIn - entry.BankOut;
                entry.RunningBalance = bankRunningBal;
            }

            // Latest first
            allBankEntries = allBankEntries.OrderByDescending(x => x.Date).ToList();

            var totalBankIn = allBankEntries.Sum(x => x.BankIn);
            var totalBankOut = allBankEntries.Sum(x => x.BankOut);

            // ================================================================
            // 🧩 BUILD VIEW MODEL
            // ================================================================
            var vm = new DashboardVM
            {
                // Sales
                TotalSales = totalSales,
                ThisMonthSales = thisMonthSales,
                LastMonthSales = lastMonthSales,

                // Purchases
                TotalPurchases = totalPurchases,
                ThisMonthPurchases = thisMonthPurchases,
                LastMonthPurchases = lastMonthPurchases,

                // Discounts
                ThisMonthDiscount = thisMonthDiscount,
                LastMonthDiscount = lastMonthDiscount,

                // Profit
                ThisMonthProfit = thisMonthProfit,
                LastMonthProfit = lastMonthProfit,

                // Qty
                ThisMonthSalesQty = thisMonthSalesQty,
                LastMonthSalesQty = lastMonthSalesQty,

                // Customer Metrics
                ThisMonthAvgCustomerSales = thisMonthAvgCustomerSales,
                LastMonthAvgCustomerSales = lastMonthAvgCustomerSales,
                ThisMonthAvgUnitsPerCustomer = thisMonthAvgUnitsPerCustomer,
                LastMonthAvgUnitsPerCustomer = lastMonthAvgUnitsPerCustomer,

                // Sales Returns
                ThisMonthSalesReturn = thisMonthSalesReturn,
                LastMonthSalesReturn = lastMonthSalesReturn,

                // Stock Overview
                TotalStockValue = Math.Round(totalStockValue, 0),
                StockValueDetails = stockValueDetails,
                BelowMinimumStockCount = belowMinimumStockCount,
                BelowMinStockDetails = belowMinStockDetails,
                AboveMaximumStockCount = aboveMaximumStockCount,
                AboveMaxStockDetails = aboveMaxStockDetails,
                DeadStockCount = deadStockCount,
                DeadStockDetails = deadStockDetails,
                ShortExpiryStockCount = shortExpiryCount,
                ShortExpiryDetails = shortExpiryDetails,
                TotalNearExpiryAmount = totalNearExpiryAmount,

                // Cash & Bank
                TotalCash = totalCash,
                CashInHandDetails = allCashEntries,
                TotalCashIn = totalCashIn,
                TotalCashOut = totalCashOut,

                TotalBank = totalBank,
                BankDetails = allBankEntries,
                TotalBankIn = totalBankIn,
                TotalBankOut = totalBankOut,


                // PDC / Cheque
                WeeklyPDCAmt = weeklyChequeAmount,
                WeeklyPDCCount = weeklyChequeCount,
                PDCDetails = pdcDetails,

                // Payables & Receivables
                TotalPayable = totalPayable,
                PayableDetails = payableDetails,
                //TotalReceiveable = salesData.Sum(x => x.Balance),
                TotalReceiveable = totalReceiveable, 
                ReceivableDetails = receivableDetails,


                Top10Items = top10Items,

                CategoryWiseSales = categoryWiseSales,
                CompanyWiseSales = companyWiseSales,
                MonthlySalesProfit = monthlySalesProfit,
                PaymentModeWiseCollection = paymentModeWiseCollection,
                CategoryStockVsSales = categoryStockVsSales,

                // Chart Data
                SalesLabels = monthlySales.Select(x => x.MonthName).ToList(),
                SalesTotals = monthlySales.Select(x => x.Total).ToList(),
                PurchaseLabels = monthlyPurchase.Select(x => x.MonthName).ToList(),
                PurchaseTotals = monthlyPurchase.Select(x => x.Total).ToList(),
                PendingPayableLabels = monthlyPendingChart.Select(x => x.MonthName).ToList(),
                PendingPayableTotals = monthlyPendingChart.Select(x => x.TotalPending).ToList(),
                MonthlysalesPurchase = monthlySalesPurchaseList
            };

            return View(vm);
        }
        // Keep dashboard sales math centralized so cards, charts and detail tables always use the same rules.
        private static decimal GetItemConversion(SalesItem salesItem)
        {
            return salesItem.ItemMaster != null && salesItem.ItemMaster.Conversion != 0
                ? salesItem.ItemMaster.Conversion
                : 1;
        }

        private static decimal CalculateSalesQuantity(SalesItem salesItem, bool isTabletWise)
        {
            if (!isTabletWise)
            {
                return salesItem.Qty;
            }

            var conversion = GetItemConversion(salesItem);
            var fullStrip = (int)salesItem.Qty;
            var fractional = salesItem.Qty - fullStrip;

            return (fullStrip * conversion) + (fractional * conversion);
        }

        private static decimal CalculateSalesBaseAmount(SalesItem salesItem, bool isTabletWise)
        {
            if (!isTabletWise)
            {
                return salesItem.Qty * salesItem.StripRate;
            }

            var conversion = GetItemConversion(salesItem);
            var quantityInBaseUnits = CalculateSalesQuantity(salesItem, true);
            var ratePerTablet = conversion == 0 ? 0 : salesItem.StripRate / conversion;

            return quantityInBaseUnits * ratePerTablet;
        }

        private static decimal CalculateFinalSalesAmount(Sales sale, SalesItem salesItem, bool isTabletWise)
        {
            var baseAmount = CalculateSalesBaseAmount(salesItem, isTabletWise);
            var afterItemDiscount = baseAmount - (baseAmount * salesItem.Discount / 100);
            var afterBillDiscount = afterItemDiscount - (afterItemDiscount * sale.discountPercent / 100);
            var gstAmount = afterBillDiscount * (salesItem.Gst / 100);
            var cessAmount = afterBillDiscount * (salesItem.Cess / 100);

            return afterBillDiscount + gstAmount + cessAmount;
        }

        private static decimal CalculateSalesCostAmount(
            SalesItem salesItem,
            bool isTabletWise,
            IReadOnlyDictionary<int, decimal> latestPurchaseCostDict)
        {
            var costRate = latestPurchaseCostDict.GetValueOrDefault(salesItem.ItemMasterId);

            if (!isTabletWise)
            {
                return salesItem.Qty * costRate;
            }

            var conversion = GetItemConversion(salesItem);
            var quantityInBaseUnits = CalculateSalesQuantity(salesItem, true);
            var costPerTablet = conversion == 0 ? 0 : costRate / conversion;

            return quantityInBaseUnits * costPerTablet;
        }

        private static decimal CalculateSalesProfitAmount(
            Sales sale,
            SalesItem salesItem,
            bool isTabletWise,
            IReadOnlyDictionary<int, decimal> latestPurchaseCostDict)
        {
            return CalculateFinalSalesAmount(sale, salesItem, isTabletWise)
                - CalculateSalesCostAmount(salesItem, isTabletWise, latestPurchaseCostDict);
        }

        private static bool IsCashPayment(string? paymentModeName)
        {
            return string.Equals(paymentModeName, "cash", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetPaymentAmount<TDocument>(
            IEnumerable<TDocument> documents,
            Func<TDocument, IEnumerable<SalsePaymentDetails>> selector,
            bool isCash)
        {
            return documents
                .SelectMany(selector)
                .Where(payment => payment.ModeOfPayment != null && IsCashPayment(payment.ModeOfPayment.Name) == isCash)
                .Sum(payment => payment.Amount);
        }
        [HttpGet]
        public async Task<IActionResult> SoldProfitDebug()
        {
            var tenantId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var salesdata = await _salesRepo.GetAll();
            var purchasedata = await _purchaseRepo.GetAll();

            // Latest purchase cost per Item
            var latestPurchaseCost = purchasedata
                .SelectMany(p => p.PurchaseItems)
                .GroupBy(pi => pi.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    CostRate = g
                        .OrderByDescending(x => x.Id) // or Purchase.BillDate
                        .First().BatchWiseCose
                })
                .ToList();

            // Join sale with latest cost
            var result =
                from si in salesdata.SelectMany(s => s.SalesItems)
                join pc in latestPurchaseCost
                    on si.ItemMasterId equals pc.ItemId

                select new
                {
                    si.ItemMasterId,
                    si.Qty,
                    SalesRate = si.Rate,
                    CostRate = pc.CostRate,
                    Profit = (si.Rate - pc.CostRate) * si.Qty
                };

            return Json(result.Take(50).ToList());
        }

        //private List<(string MonthName, decimal Total)> GroupMonthlyData(IEnumerable<(DateTime? BillDate, decimal TotalPayable)> data)
        //{
        //    return data
        //        .Where(x => x.BillDate.HasValue)
        //        .GroupBy(x => x.BillDate.Value.Month)
        //        .Select(g => (
        //            CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
        //            g.Sum(x => x.TotalPayable)))
        //        .OrderBy(x => DateTime.ParseExact(x.Item1, "MMM", CultureInfo.CurrentCulture).Month)
        //        .ToList();
        //}
        private List<(string MonthName, decimal Total)> GroupMonthlyData(IEnumerable<(DateTime? BillDate, decimal TotalPayable)> data)
        {
            return data
                .Where(x => x.BillDate.HasValue)
                .GroupBy(x => new
                {
                    Year = x.BillDate.Value.Year,
                    Month = x.BillDate.Value.Month
                })
                .Select(g => (
                    MonthName: new DateTime(g.Key.Year, g.Key.Month, 1)
                                    .ToString("MMM yyyy"),
                    Total: g.Sum(x => x.TotalPayable)
                ))
                .OrderBy(x => DateTime.ParseExact(
                    x.MonthName,
                    "MMM yyyy",
                    CultureInfo.InvariantCulture))
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
            var expiryToDate = today.AddMonths(6);

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

        public IActionResult Privacy() => View();
    }

}
