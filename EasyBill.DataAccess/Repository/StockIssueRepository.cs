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
        private readonly AOne.DataAccess.Data.ApplicationDbContext _context;
        public StockIssueRepository(IUnitOfWork unitofwork, AOne.DataAccess.Data.ApplicationDbContext context)
        {
            _unitofwork = unitofwork;
            _context = context;
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

        public async Task<IList<StockIssue>> GetPendingTransfersForBranch(string tenantId)
        {
            try
            {
                // We MUST use _context directly instead of _unitofwork.GetRepository<StockIssue>().Query()
                // because the Repository automatically injects a ".Where(x => x.TenantId == CURRENT_TENANT)" filter.
                // Since this StockIssue was created by the ISSUING branch, its TenantId is the Issuing branch's ID.
                // The RECEIVING branch is querying it, so if the repository filter applies, it will return 0 results!
                // IgnoreQueryFilters() only ignores EF Core global filters (like Deleted), NOT the manual Repository filter.
                
                // Get transfers for the last 30 days to avoid loading too much historical data.
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                
                IList<StockIssue> results = await _context.StockIssues
                    .IgnoreQueryFilters()
                    .Include(x => x.Tenant) // Issuing branch
                    .Include(x => x.StockIssuesItems)
                    .Where(x => x.TransferToTenantId == tenantId && x.Deleted == null && !x.IsReceived && x.TransferStatus == "Pending" && x.ChallanDate >= thirtyDaysAgo)
                    .OrderByDescending(x => x.ChallanDate)
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

        public async Task<StockIssue> GetByIdBypassTenant(int id)
        {
            try
            {
                return await _context.StockIssues
                    .IgnoreQueryFilters()
                    .Include(x => x.Tenant)
                    .Include(x => x.StockIssuesItems)
                        .ThenInclude(i => i.ItemMaster)
                    .FirstOrDefaultAsync(x => x.Id == id && x.Deleted == null);
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
