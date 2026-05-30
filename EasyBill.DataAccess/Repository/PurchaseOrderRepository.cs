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
                IList<PurchaseOrder> results = await repository.Query().Include(x => x.Suppliers).Include(x => x.PurchaseOrderItems).ThenInclude(x => x.ItemMasters).ToListAsync();

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
