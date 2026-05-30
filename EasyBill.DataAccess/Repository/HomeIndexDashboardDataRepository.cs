using AOne.DataAccess.Data;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EasyBill.DataAccess.Repository
{
    public sealed class HomeIndexDashboardDataRepository : StoredProcedureRepositoryBase, IHomeIndexDashboardDataRepository
    {
        private readonly ISalesRepository _salesRepo;
        private readonly IPurchaseRepository _purchaseRepo;
        private readonly IItemMasterRepository _itemMasterRepo;
        public HomeIndexDashboardDataRepository(
            ISalesRepository salesRepo,
            IPurchaseRepository purchaseRepo,
            IItemMasterRepository itemMasterRepo,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor)
            : base(dbContext, httpContextAccessor, tenantAccessor)
        {
            _salesRepo = salesRepo;
            _purchaseRepo = purchaseRepo;
            _itemMasterRepo = itemMasterRepo;
        }

        public async Task<HomeIndexCoreDatasets> LoadCoreDatasetsAsync(
             DateTime startDate,
             DateTime endDate,
             CancellationToken cancellationToken = default)
        {
            var sales = await _salesRepo.GetAll();
            var purchase = await _purchaseRepo.GetAll();
            var items = await _itemMasterRepo.GetAll();
            var sp = await WithStoredProcedureCommandAsync(
                "dbo.usp_Dashboard_LoadHomeIndexRest",
                async command =>
                {
                    AddParameter(command, "@StartDate", startDate.Date, DbType.Date);
                    AddParameter(command, "@EndDate", endDate.Date, DbType.Date);
                    AddFilterParameters(command);
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    return await DashboardHomeIndexDatasetReader.ReadAsync(reader, cancellationToken);
                },
                cancellationToken);

            return new HomeIndexCoreDatasets
            {
                Sales = sales,
                Purchase = purchase,
                AllItems = items,
                PurchaseChallans = sp.PurchaseChallans,
                Payments = sp.Payments,
                PurchaseReturns = sp.PurchaseReturns,
                StockIssues = sp.StockIssues,
                StockReturns = sp.StockReturnsFiltered,
                StockReceives = sp.StockReceives,
                StockReturnsAll = sp.StockReturnsAll,
                Categories = sp.Categories,
                Companies = sp.Companies
            };
        }
    }
}
