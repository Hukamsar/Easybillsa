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
    public class SupplierAdvanceRepository : ISupplierAdvanceRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SupplierAdvanceRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<SupplierAdvance> Create(SupplierAdvance model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SupplierAdvance>();
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
        public async Task<IList<SupplierAdvance>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<SupplierAdvance>();
                IList<SupplierAdvance> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SupplierAdvance> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SupplierAdvance>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SupplierAdvance> Update(SupplierAdvance model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SupplierAdvance>();
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
        public async Task Delete(SupplierAdvance model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<SupplierAdvance>();
                batchRepository.Delete(model);
                using (var transaction = batchRepository.BeginTransaction())
                {
                    await batchRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<SupplierAdvance>> GetBySupplierId(int? supplierId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SupplierAdvance>();
                IList<SupplierAdvance> results = await repository.Query().Where(x => x.SupplierId == supplierId).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
