using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class PurchaseItemRepository : IPurchaseItemRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PurchaseItemRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PurchaseItem> Create(PurchaseItem model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseItem>();
                repository.Add(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public async Task<IList<PurchaseItem>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseItem>();
                IList<PurchaseItem> results = await repository.Query().Include(x => x.Purchases).Include(x => x.ItemMasters).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<PurchaseItem>> GetByItemMasterId(int itemmasterId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseItem>();
                IList<PurchaseItem> results = await repository.Query().Include(x => x.Purchases).Include(x => x.ItemMasters).Where(x => x.ItemId == itemmasterId).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PurchaseItem> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseItem>();
                var result = await repository.Query().Include(x => x.Purchases).Include(x => x.ItemMasters).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseItem> Update(PurchaseItem model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseItem>();
                repository.Update(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }

                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task Delete(PurchaseItem model)
        {
            try
            {
                var purchaseRepository = _unitofwork.GetRepository<PurchaseItem>();
                purchaseRepository.Delete(model);
                using (var transaction = purchaseRepository.BeginTransaction())
                {
                    await purchaseRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
