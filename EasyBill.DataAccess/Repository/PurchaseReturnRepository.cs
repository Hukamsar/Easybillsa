using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class PurchaseReturnRepository : IPurchaseReturnRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PurchaseReturnRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PurchaseReturn> Create(PurchaseReturn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseReturn>();
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

        public async Task CreateRange(List<PurchaseReturn> purchasereturn)
        {
            try
            {
                var purchaseRepo = _unitofwork.GetRepository<PurchaseReturn>(); 

                using (var transaction = purchaseRepo.BeginTransaction())
                { 

                    if (purchasereturn != null && purchasereturn.Count > 0)
                    {
                        purchaseRepo.AddRange(purchasereturn);

                    }
                    await purchaseRepo.SaveChangesAsync();
                    transaction.Commit();
                } 
            }
            catch (Exception ex)
            {
                throw new Exception("CreateWithStockAsync failed: " + ex.Message);
            }
        }

        public async Task<IList<PurchaseReturn>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseReturn>();
                IList<PurchaseReturn> results = await repository.Query().Include(x => x.PurchaseReturnItems).Include(x => x.Suppliers).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<PurchaseReturn>> GetBySupplierId(int supplierId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseReturn>();
                IList<PurchaseReturn> results = await repository.Query().Include(x => x.PurchaseReturnItems).Include(x => x.Suppliers).Where(x => x.SupplierId == supplierId).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseReturn> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseReturn>();
                var result = await repository.Query().Include(x => x.Suppliers).Include(x => x.PurchaseReturnItems).ThenInclude(x => x.ItemMasters).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseReturn> Update(PurchaseReturn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseReturn>();
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
        public async Task UpdateStocksAsync(List<PurchaseReturn> stockList)
        {
            var repository = _unitofwork.GetRepository<PurchaseReturn>();
           
            await repository.SaveChangesAsync();
            try
            { 
                using (var transaction = repository.BeginTransaction())
                {
                    repository.UpdateRange(stockList);
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                } 
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task Delete(PurchaseReturn model)
        {
            try
            {
                var purchaseRepository = _unitofwork.GetRepository<PurchaseReturn>();
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
