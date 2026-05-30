using AOne.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class StockReturnRepository : IStockReturnRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public StockReturnRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<StockReturn> Create(StockReturn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();
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

        public async Task Delete(StockReturn model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<StockReturn>();
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

        public async Task<IList<StockReturn>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();
                IList<StockReturn> results = await repository.Query().Include(x => x.Customers).Include(x => x.StockReturnItems).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<StockReturn>> GetByCustomerId(int? customerId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();
                IList<StockReturn> result = await repository.Query().Include(x => x.Customers).Include(x => x.PharmacyDoctor).Include(x => x.StockReturnItems).ThenInclude(x => x.ItemMaster).Where(l => l.CustomerId == customerId).ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<StockReturn> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();
                var result = await repository.Query().Include(x => x.Customers).Include(x => x.PharmacyDoctor).Include(x => x.StockReturnItems).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<StockReturn> Update(StockReturn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();
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

        public async Task<List<SelectListItem>> GetCustomersWithReturns()
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReturn>();

                var customers = await repository.Query()
                    .Include(x => x.Customers)
                    .Where(x => x.Customers != null) // Safety check
                    .Select(x => x.Customers)
                    .Distinct()
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToListAsync();

                return customers;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
