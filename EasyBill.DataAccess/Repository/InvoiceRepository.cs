using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public InvoiceRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Invoice>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Invoice>();
                IList<Invoice> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Invoice> Create(Invoice model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Invoice>();
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
        public async Task<Invoice> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Invoice>();
                var result = await repository.Query().Include(x => x.invoiceItems).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Invoice> Update(Invoice model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Invoice>();
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
        public async Task Delete(Invoice model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Invoice>();
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
