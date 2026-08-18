using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class StoreRankingFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Zone { get; set; }
        public string? Region { get; set; }
        public int? StateId { get; set; }
        public string? State { get; set; }
        public int? CityId { get; set; }
        public string? City { get; set; }
        public string? Cluster { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? StoreType { get; set; }
        public string? Company { get; set; }
    }

    public class StoreRankingReportDto
    {
        public StoreRankingKpiDto Kpis { get; set; } = new StoreRankingKpiDto();
        public List<StoreRankingGridDto> GridData { get; set; } = new List<StoreRankingGridDto>();
    }

    public class StoreRankingKpiDto
    {
        public decimal TotalSalesValue { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public decimal SalesGrowthPercentage { get; set; }
        public int NumberOfBills { get; set; }
        public int CustomerFootfall { get; set; }
        public decimal AverageBillValue { get; set; }
        public int ItemsSold { get; set; }
        public decimal SalesPerSquareFoot { get; set; }
        public decimal SalesPerEmployee { get; set; }
    }

    public class StoreRankingGridDto
    {
        public int Rank { get; set; }
        public string StoreCode { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal GpPercentage { get; set; }
        public decimal NetProfit { get; set; }
        public int NumberOfBills { get; set; }
        public decimal AvgBillValue { get; set; }
        public int Customers { get; set; }
        public decimal GrowthPercentage { get; set; }
        public decimal InventoryTurnover { get; set; }
        public decimal StockValue { get; set; }
        public decimal RankScore { get; set; }
    }
}
