using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using EasyBill.DataAccess.Repository.IRepository;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace EasyBill.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoDashboardApiController : ControllerBase
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly ISalesRepository _salesRepo;
        private readonly ApplicationDbContext _dbContext;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public HoDashboardApiController(
            ITenantRepository tenantRepo, 
            ISalesRepository salesRepo,
            ApplicationDbContext dbContext,
            Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _tenantRepo = tenantRepo;
            _salesRepo = salesRepo;
            _dbContext = dbContext;
            _cache = cache;
        }

        [HttpGet("summary-report")]
        public async Task<IActionResult> GetSummaryReport(string hoTenantId)
        {
            if (string.IsNullOrEmpty(hoTenantId))
                return BadRequest(new { success = false, message = "HO TenantId is required." });

            var cacheKey = $"DashboardSummary_{hoTenantId}";
            
            var responseData = await _cache.GetOrCreateAsync<object>(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);

                var allTenants = await _tenantRepo.GetAll();
                var branches = allTenants
                    .Where(t => t.ParentTenantId == hoTenantId || t.Id == hoTenantId) // Includes HO itself and its branches
                    .Select(t => t.Id)
                    .ToList();

                if (!branches.Any())
                {
                    return new { 
                        success = true, 
                        data = new List<object>(), 
                        kpis = new {
                            thisMonthSales = 0M,
                            lastMonthSales = 0M,
                            thisMonthPurchases = 0M,
                            lastMonthPurchases = 0M,
                            thisMonthProfit = 0M,
                            lastMonthProfit = 0M,
                            thisMonthDiscount = 0M,
                            lastMonthDiscount = 0M,
                            thisMonthSalesQty = 0,
                            lastMonthSalesQty = 0,
                            thisMonthAvgCustomerSales = 0D,
                            lastMonthAvgCustomerSales = 0D,
                            thisMonthAvgUnitsPerCustomer = 0D,
                            lastMonthAvgUnitsPerCustomer = 0D,
                            thisMonthSalesReturn = 0M,
                            lastMonthSalesReturn = 0M
                        }
                    };
                }

                var today = DateTime.Today;
                var startOfYear = new DateTime(today.Year, 1, 1);
                var startOfLastYear = new DateTime(today.Year - 1, 1, 1);
                var endOfLastYear = new DateTime(today.Year - 1, 12, 31);

                var thisMonthStart = new DateTime(today.Year, today.Month, 1).Date;
                var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1).Date;
                var lastMonthStart = thisMonthStart.AddMonths(-1).Date;
                var lastMonthEnd = thisMonthStart.AddDays(-1).Date;

                // 1. Fetch sales within the 2-month window
                var salesRecent = await _dbContext.Saless
                    .Where(s => branches.Contains(s.TenantId) && s.Deleted == null && s.BillDate >= lastMonthStart && s.BillDate <= thisMonthEnd)
                    .Select(s => new {
                        s.TenantId,
                        s.BillDate,
                        s.TotalPayable,
                        s.Totaldiscount,
                        s.CustomerId,
                        SalesItems = s.SalesItems
                            .Where(si => si.Deleted == null)
                            .Select(si => new { si.ItemMasterId, si.Qty })
                    })
                    .ToListAsync();

                // 2. Fetch sales totals from start of last year
                var salesTotals = await _dbContext.Saless
                    .Where(s => branches.Contains(s.TenantId) && s.Deleted == null && s.BillDate >= startOfLastYear)
                    .Select(s => new {
                        s.TenantId,
                        s.BillDate,
                        s.TotalPayable
                    })
                    .ToListAsync();

                // 3. Fetch latest purchase cost for sold items only
                var soldItemIds = salesRecent
                    .SelectMany(s => s.SalesItems)
                    .Select(si => si.ItemMasterId)
                    .Distinct()
                    .ToList();

                var purchaseCostsList = await _dbContext.PurchaseItems
                    .Where(pi => branches.Contains(pi.TenantId) && pi.Deleted == null && soldItemIds.Contains(pi.ItemId))
                    .Select(pi => new { pi.ItemId, pi.Id, pi.BatchWiseCose })
                    .ToListAsync();

                var latestPurchaseCost = purchaseCostsList
                    .GroupBy(x => x.ItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.Id).FirstOrDefault()?.BatchWiseCose ?? 0M
                    );

                // 4. Fetch purchases within the 2-month window
                var allPurchases = await _dbContext.Purchases
                    .Where(p => branches.Contains(p.TenantId) && p.Deleted == null && p.BillDate >= lastMonthStart && p.BillDate <= thisMonthEnd)
                    .Select(p => new { p.TenantId, p.BillDate, p.TotalPayable })
                    .ToListAsync();

                // 5. Fetch stock returns within the 2-month window
                var salesReturns = await _dbContext.StockReturns
                    .Where(x => branches.Contains(x.TenantId) && x.ChallanDate >= lastMonthStart && x.ChallanDate <= thisMonthEnd)
                    .Select(x => new { x.ChallanDate, x.TotalPayable })
                    .ToListAsync();

                // Consolidated KPIs using salesRecent and allPurchases
                var thisMonthSales = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                    .Sum(s => s.TotalPayable);

                var lastMonthSales = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                    .Sum(s => s.TotalPayable);

                var thisMonthPurchases = allPurchases
                    .Where(p => p.BillDate.HasValue && p.BillDate.Value.Date >= thisMonthStart && p.BillDate.Value.Date <= thisMonthEnd)
                    .Sum(p => p.TotalPayable);

                var lastMonthPurchases = allPurchases
                    .Where(p => p.BillDate.HasValue && p.BillDate.Value.Date >= lastMonthStart && p.BillDate.Value.Date <= lastMonthEnd)
                    .Sum(p => p.TotalPayable);

                var thisMonthDiscount = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                    .Sum(s => s.Totaldiscount);

                var lastMonthDiscount = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                    .Sum(s => s.Totaldiscount);

                var thisMonthProfit = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                    .Sum(s => s.TotalPayable - s.SalesItems.Sum(si => si.Qty * latestPurchaseCost.GetValueOrDefault(si.ItemMasterId, 0M)));

                var lastMonthProfit = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                    .Sum(s => s.TotalPayable - s.SalesItems.Sum(si => si.Qty * latestPurchaseCost.GetValueOrDefault(si.ItemMasterId, 0M)));

                // Additional Vertical Card Metrics using salesRecent
                var thisMonthSalesQty = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                    .SelectMany(s => s.SalesItems)
                    .Sum(si => si.Qty);

                var lastMonthSalesQty = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                    .SelectMany(s => s.SalesItems)
                    .Sum(si => si.Qty);

                var thisMonthCustomerCount = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                    .Select(s => s.CustomerId).Distinct().Count();

                var lastMonthCustomerCount = salesRecent
                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                    .Select(s => s.CustomerId).Distinct().Count();

                var thisMonthAvgCustomerSales = thisMonthCustomerCount > 0
                    ? Math.Round((double)thisMonthSales / thisMonthCustomerCount, 2) : 0;

                var lastMonthAvgCustomerSales = lastMonthCustomerCount > 0
                    ? Math.Round((double)lastMonthSales / lastMonthCustomerCount, 2) : 0;

                var thisMonthAvgUnitsPerCustomer = thisMonthCustomerCount > 0
                    ? Math.Round((double)thisMonthSalesQty / thisMonthCustomerCount, 1) : 0;

                var lastMonthAvgUnitsPerCustomer = lastMonthCustomerCount > 0
                    ? Math.Round((double)lastMonthSalesQty / lastMonthCustomerCount, 1) : 0;

                var thisMonthSalesReturn = salesReturns
                    .Where(x => x.ChallanDate.HasValue && x.ChallanDate.Value.Date >= thisMonthStart && x.ChallanDate.Value.Date <= thisMonthEnd)
                    .Sum(x => x.TotalPayable);

                var lastMonthSalesReturn = salesReturns
                    .Where(x => x.ChallanDate.HasValue && x.ChallanDate.Value.Date >= lastMonthStart && x.ChallanDate.Value.Date <= lastMonthEnd)
                    .Sum(x => x.TotalPayable);

                var report = branches.Select(branchId => {
                    var branchSalesRecent = salesRecent.Where(s => s.TenantId == branchId).ToList();
                    var branchSalesTotals = salesTotals.Where(s => s.TenantId == branchId).ToList();
                    return new
                    {
                        OutletId = branchId,
                        OutletName = allTenants.FirstOrDefault(t => t.Id == branchId)?.Name ?? "Unknown",

                        TodaysSales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date == today)
                            .Sum(s => s.TotalPayable),

                        YesterdaysSales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date == today.AddDays(-1))
                            .Sum(s => s.TotalPayable),

                        MonthlySales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Month == today.Month && s.BillDate.Value.Year == today.Year)
                            .Sum(s => s.TotalPayable),

                        YTDSales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= startOfYear && s.BillDate.Value.Date <= today)
                            .Sum(s => s.TotalPayable),

                        LYTDSales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= startOfLastYear && s.BillDate.Value.Date <= endOfLastYear)
                            .Sum(s => s.TotalPayable),

                        LastMonthSales = branchSalesTotals
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= lastMonthStart && s.BillDate.Value.Date <= lastMonthEnd)
                            .Sum(s => s.TotalPayable),

                        SalesQty = branchSalesRecent
                            .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date >= thisMonthStart && s.BillDate.Value.Date <= thisMonthEnd)
                            .SelectMany(s => s.SalesItems)
                            .Sum(si => si.Qty),

                        DailyHistory = Enumerable.Range(0, 30)
                            .Select(offset => today.AddDays(-29 + offset))
                            .Select(date => new {
                                DateName = date.ToString("dd MMM"),
                                Amount = branchSalesTotals
                                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date == date)
                                    .Sum(s => s.TotalPayable)
                            })
                            .ToList(),

                        MonthlyHistory = Enumerable.Range(0, 6)
                            .Select(offset => new DateTime(today.Year, today.Month, 1).AddMonths(-5 + offset))
                            .Select(m => new {
                                MonthName = m.ToString("MMM yy"),
                                Amount = branchSalesTotals
                                    .Where(s => s.BillDate.HasValue && s.BillDate.Value.Month == m.Month && s.BillDate.Value.Year == m.Year)
                                    .Sum(s => s.TotalPayable)
                            })
                            .ToList(),

                        SalesHistory = Enumerable.Range(0, 7)
                            .Select(offset => today.AddDays(-6 + offset))
                            .Select(date => branchSalesTotals
                                .Where(s => s.BillDate.HasValue && s.BillDate.Value.Date == date)
                                .Sum(s => s.TotalPayable))
                            .ToList()
                    };
                }).ToList();

                return new { 
                    success = true, 
                    data = report,
                    kpis = new {
                        thisMonthSales,
                        lastMonthSales,
                        thisMonthPurchases,
                        lastMonthPurchases,
                        thisMonthProfit,
                        lastMonthProfit,
                        thisMonthDiscount,
                        lastMonthDiscount,
                        thisMonthSalesQty,
                        lastMonthSalesQty,
                        thisMonthAvgCustomerSales,
                        lastMonthAvgCustomerSales,
                        thisMonthAvgUnitsPerCustomer,
                        lastMonthAvgUnitsPerCustomer,
                        thisMonthSalesReturn,
                        lastMonthSalesReturn
                    }
                };
            });

            return Ok(responseData);
        }

        [HttpGet("analytical-report")]
        public async Task<IActionResult> GetAnalyticalReport(string hoTenantId)
        {
            var allTenants = await _tenantRepo.GetAll();
            var branches = allTenants
                .Where(t => t.ParentTenantId == hoTenantId) // Excludes HO itself
                .Select(t => t.Id)
                .ToList();

            if (!branches.Any())
                return NotFound(new { message = "No branches found for this Head Office." });

            // Fetch sales from database
            var salesList = await _dbContext.Saless
                .Where(s => branches.Contains(s.TenantId) && s.Deleted == null)
                .ToListAsync();

            // 1. Sales by Payment Mode
            var paymentModeDistribution = salesList
                .GroupBy(s => s.PaymentType ?? "Cash")
                .Select(g => new
                {
                    Mode = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(s => s.TotalPayable)
                })
                .ToList();

            // 2. High Discount Exception Alerts
            var highDiscounts = salesList
                .Where(s => s.discountPercent > 10 || s.discountAmount > 500)
                .Select(s => new
                {
                    s.Id,
                    s.BillNo,
                    s.BillDate,
                    OutletName = allTenants.FirstOrDefault(t => t.Id == s.TenantId)?.Name ?? "Unknown",
                    s.TotalPayable,
                    Discount = s.discountAmount,
                    DiscountPercent = s.discountPercent
                })
                .OrderByDescending(s => s.Discount)
                .Take(10)
                .ToList();

            // 3. Top Selling Items
            var topItems = await _dbContext.SalesItems
                .Include(si => si.ItemMaster)
                .Where(si => branches.Contains(si.TenantId) && si.Deleted == null)
                .GroupBy(si => si.ItemMaster.Name)
                .Select(g => new
                {
                    ItemName = g.Key,
                    QtySold = g.Sum(si => si.Qty),
                    TotalRevenue = g.Sum(si => si.Amount)
                })
                .OrderByDescending(g => g.TotalRevenue)
                .Take(5)
                .ToListAsync();

            // 4. Low Stock Alerts
            var lowStockDbList = await _dbContext.Stocks
                .Include(st => st.ItemMaster)
                .Where(st => branches.Contains(st.ItemMaster.TenantId) && st.Stocks <= st.ItemMaster.MinimumQty)
                .Take(10)
                .ToListAsync();

            var lowStockAlerts = lowStockDbList.Select(st => new
            {
                st.Id,
                ItemName = st.ItemMaster.Name,
                ItemCode = st.ItemCode,
                OutletName = allTenants.FirstOrDefault(t => t.Id == st.ItemMaster.TenantId)?.Name ?? "Unknown",
                CurrentStock = st.Stocks,
                MinQty = st.ItemMaster.MinimumQty
            }).ToList();

            return Ok(new
            {
                success = true,
                paymentModeDistribution,
                highDiscounts,
                topItems,
                lowStockAlerts
            });
        }

        [HttpGet("sales-list")]
        public async Task<IActionResult> GetSalesList(string hoTenantId)
        {
            var allTenants = await _tenantRepo.GetAll();
            var branches = allTenants
                .Where(t => t.ParentTenantId == hoTenantId) // Excludes HO itself
                .Select(t => t.Id)
                .ToList();

            if (!branches.Any())
                return NotFound(new { message = "No branches found for this Head Office." });

            var salesList = await _dbContext.Saless
                .Where(s => branches.Contains(s.TenantId) && s.Deleted == null)
                .OrderByDescending(s => s.BillDate)
                .Take(50)
                .ToListAsync();

            var result = salesList.Select(s => new
            {
                s.Id,
                s.BillNo,
                s.BillDate,
                OutletName = allTenants.FirstOrDefault(t => t.Id == s.TenantId)?.Name ?? "Unknown",
                TenantId = s.TenantId,
                s.MobileNo,
                s.TotalPayable,
                s.PaymentType,
                s.PaymentStatus
            }).ToList();

            return Ok(new { success = true, data = result });
        }
    }
}