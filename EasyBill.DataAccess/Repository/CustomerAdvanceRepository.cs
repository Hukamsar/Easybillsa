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
    public class CustomerAdvanceRepository : ICustomerAdvanceRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public CustomerAdvanceRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<CustomerAdvance> Create(CustomerAdvance model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CustomerAdvance>();
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
        public async Task<IList<CustomerAdvance>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<CustomerAdvance>();
                IList<CustomerAdvance> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<CustomerAdvance> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CustomerAdvance>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<CustomerAdvance> Update(CustomerAdvance model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CustomerAdvance>();
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
        public async Task Delete(CustomerAdvance model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<CustomerAdvance>();
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
        public async Task<IList<CustomerAdvance>> GetByCustomerId(int? customerid)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CustomerAdvance>();
                IList<CustomerAdvance> results = await repository.Query().Where(x => x.CustomerId == customerid).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
