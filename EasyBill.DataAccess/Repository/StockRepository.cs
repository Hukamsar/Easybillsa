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
    public class StockRepository : IStockRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public StockRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Stock> Create(Stock model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Stock>();
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
        public async Task  CreateWithStockAsync( List<Stock> stocks)
        {
            try
            { 
                var stockRepo = _unitofwork.GetRepository<Stock>(); 
                using (var transaction = stockRepo.BeginTransaction())
                {  
                    if (stocks != null && stocks.Count > 0)
                    {
                        stockRepo.AddRange(stocks);
                        await stockRepo.SaveChangesAsync();
                    }

                    transaction.Commit();
                } 
            }
            catch (Exception ex)
            {
                throw new Exception("CreateWithStockAsync failed: " + ex.Message);
            }
        }
        public async Task Delete(Stock model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Stock>();
                assetGroupRepository.Delete(model);
                using (var transaction = assetGroupRepository.BeginTransaction())
                {
                    await assetGroupRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IList<Stock>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Stock>();
                IList<Stock> results = await repository.Query().Include(x => x.ItemMaster).Include(x => x.Category).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Stock> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Stock>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Stock> Update(Stock model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Stock>();
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
        public async Task UpdateRangeStocksAsync(List<Stock> stockList)
        {  
            try
            {
                var repository = _unitofwork.GetRepository<Stock>(); 
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
    }
}
