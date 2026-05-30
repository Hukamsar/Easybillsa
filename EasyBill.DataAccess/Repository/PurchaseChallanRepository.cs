using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class PurchaseChallanRepository : IPurchaseChallanRepository
    {
        private readonly IUnitOfWork _unitofwork;

        private readonly IStockService _stockService;

        public PurchaseChallanRepository(IUnitOfWork unitofwork, IStockService stockService)
        {
            _unitofwork = unitofwork;
            _stockService = stockService;
        }

        public async Task<PurchaseChallan> Create(PurchaseChallan model)
        {
            var repository = _unitofwork.GetRepository<PurchaseChallan>();
            repository.Add(model);

            using (var transaction = repository.BeginTransaction())
            {
                if (model.PurchaseChallanItems != null && model.PurchaseChallanItems.Any())
                {
                    foreach (var item in model.PurchaseChallanItems)
                    {
                        await _stockService.UpdateStock(
                            item.ItemId,
                            item.Batch ?? string.Empty,
                            item.Qty + item.FreeQty,
                            item.ExpiryDate,
                            item.Mrp,
                            item.Rate
                        );
                    }
                }

                await repository.SaveChangesAsync();
                transaction.Commit();
            }

            return model;
        }

        public async Task<IList<PurchaseChallan>> GetAll()
        {
            return await BuildQuery().ToListAsync();
        }

        public async Task<PurchaseChallan?> GetById(int? Id)
        {
            return await BuildQuery()
                .FirstOrDefaultAsync(l => l.Id == Id);
        }

        public async Task<bool> ExistsByBillNoAsync(string? billNo, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(billNo))
            {
                return false;
            }

            var normalizedBillNo = billNo.Trim().ToLower();

            return await _unitofwork
                .GetRepository<PurchaseChallan>()
                .Query()
                .AnyAsync(x =>
                    x.BillNo != null &&
                    x.BillNo.Trim().ToLower() == normalizedBillNo &&
                    (!excludeId.HasValue || x.Id != excludeId.Value));
        }

        public async Task<PurchaseChallan> Update(PurchaseChallan model)
        {
            var repository = _unitofwork.GetRepository<PurchaseChallan>();
            repository.Update(model);

            using (var transaction = repository.BeginTransaction())
            {
                await repository.SaveChangesAsync();
                transaction.Commit();
            }

            return model;
        }

        public async Task MarkConvertedAsync(int challanId, int purchaseId)
        {
            var repository = _unitofwork.GetRepository<PurchaseChallan>();
            var challan = await repository.Query().FirstOrDefaultAsync(x => x.Id == challanId);

            if (challan == null)
            {
                return;
            }

            challan.Status = "Converted";
            challan.ConvertedPurchaseId = purchaseId;
            repository.Update(challan);

            using (var transaction = repository.BeginTransaction())
            {
                await repository.SaveChangesAsync();
                transaction.Commit();
            }
        }

        public async Task Delete(PurchaseChallan model)
        {
            var repository = _unitofwork.GetRepository<PurchaseChallan>();
            repository.Delete(model);

            using (var transaction = repository.BeginTransaction())
            {
                await repository.SaveChangesAsync();
                transaction.Commit();
            }
        }

        private IQueryable<PurchaseChallan> BuildQuery()
        {
            return _unitofwork
                .GetRepository<PurchaseChallan>()
                .Query()
                .Include(x => x.Suppliers)
                .Include(x => x.PaymentDetails)
                .Include(x => x.PurchaseChallanItems)
                    .ThenInclude(x => x.ItemMasters)
                .Include(x => x.ConvertedPurchase);
        }
    }
}