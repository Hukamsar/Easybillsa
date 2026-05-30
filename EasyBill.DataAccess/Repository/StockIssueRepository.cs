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
    public class StockIssueRepository : IStockIssueRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public StockIssueRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<StockIssue>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockIssue>();
                IList<StockIssue> results = await repository.Query()
                    .Include(x => x.Customers)
                    .Include(x => x.StockIssuesItems)
                    .Include(x => x.SalsePaymentDetails)
                    .ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<StockIssue> Create(StockIssue model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockIssue>();
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
        public async Task<StockIssue> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockIssue>();
                var result = await repository.Query()
                    .Include(x => x.Customers)
                    .Include(x => x.PharmacyDoctor)
                    .Include(x => x.StockIssuesItems)
                        .ThenInclude(x => x.ItemMaster)
                    .Include(x => x.SalsePaymentDetails)
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
        public async Task<StockIssue> Update(StockIssue model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<StockIssue>();
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
        public async Task Delete(StockIssue model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<StockIssue>();
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
