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
    public class StockReceiveRepository : IStockReceiveRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public StockReceiveRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<StockReceive>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReceive>();
                IList<StockReceive> results = await repository.Query()
                    .Include(x => x.Customers)
                    .Include(x => x.Supplier)
                    .Include(x => x.StockReceiveItems)
                    .Include(x => x.PaymentDetails)
                    .ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<StockReceive> Create(StockReceive model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReceive>();
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
        public async Task<StockReceive> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReceive>();
                var result = await repository.Query()
                    .Include(x => x.Customers)
                    .Include(x => x.Supplier)
                    .Include(x => x.PharmacyDoctor)
                    .Include(x => x.StockReceiveItems)
                        .ThenInclude(x => x.ItemMaster)
                            .ThenInclude(x => x.Hsn)
                    .Include(x => x.PaymentDetails)
                        .ThenInclude(x => x.ModeOfPayment)
                    .Where(l => l.Id == Id)
                    .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<StockReceive> Update(StockReceive model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockReceive>();
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
        public async Task Delete(StockReceive model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<StockReceive>();
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
    }
}
