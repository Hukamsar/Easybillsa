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
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PurchaseOrderRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
       
        public async Task<IList<PurchaseOrder>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
                IList<PurchaseOrder> results = await repository.Query().Include(x => x.Suppliers).Include(x => x.TargetTenant).Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters).ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<PurchaseOrder>> GetIncomingRequests(string targetTenantId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
                
                // Debug log before querying
                System.IO.File.AppendAllText("F:\\NewEasyBill_Project\\debug.txt", $"Repo GetIncomingRequests Called. targetTenantId: '{targetTenantId}'\n");
                
                var allDbSrs = await repository.GetAll().IgnoreQueryFilters().Where(p => p.OrderType == "SR").ToListAsync();
                System.IO.File.AppendAllText("F:\\NewEasyBill_Project\\debug.txt", $"Repo Total SR count in DB (unfiltered): {allDbSrs.Count}\n");
                foreach (var s in allDbSrs) {
                    System.IO.File.AppendAllText("F:\\NewEasyBill_Project\\debug.txt", $"Repo SR DB -> Id: {s.Id}, Target: '{s.TargetTenantId}', Status: '{s.WorkflowStatus}'\n");
                }

                // Bypass global query filters with IgnoreQueryFilters()
                IList<PurchaseOrder> results = await repository.GetAll().IgnoreQueryFilters()
                    .Include(x => x.Suppliers)
                    .Include(x => x.TargetTenant)
                    .Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters)
                    .Where(p => p.TargetTenantId == targetTenantId && p.OrderType == "SR" && p.WorkflowStatus == "Pending")
                    .ToListAsync();
                    
                System.IO.File.AppendAllText("F:\\NewEasyBill_Project\\debug.txt", $"Repo Filtered Results count: {results.Count}\n");

                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseOrder> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
                var result = await repository.Query().Include(x => x.Suppliers).Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PurchaseOrder> GetIncomingRequestById(int id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
                var result = await repository.GetAll().IgnoreQueryFilters()
                    .Include(x => x.Suppliers)
                    .Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters)
                    .Where(l => l.Id == id)
                    .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseOrder> Create(PurchaseOrder model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
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
        public async Task<PurchaseOrder> Update(PurchaseOrder model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
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
        public async Task Delete(PurchaseOrder model)
        {
            try
            {
                var purchaseRepository = _unitofwork.GetRepository<PurchaseOrder>();
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
        public async Task<IList<PurchaseOrder>> GetBySupplierId(int supplierId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseOrder>();
                IList<PurchaseOrder> results = await repository.Query().Where(x => x.SupplierId == supplierId).Include(x => x.Suppliers).Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters).ToListAsync();

                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
