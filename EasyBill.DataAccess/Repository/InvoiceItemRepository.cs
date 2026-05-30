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
    public class InvoiceItemRepository : IInvoiceItemRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public InvoiceItemRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<InvoiceItem>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceItem>();
                IList<InvoiceItem> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<InvoiceItem> Create(InvoiceItem model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceItem>();
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
        public async Task<InvoiceItem> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceItem>();
                var result = await repository.Query().Include(x => x.Invoice).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<InvoiceItem> Update(InvoiceItem model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceItem>();
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
        public async Task Delete(InvoiceItem model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<InvoiceItem>();
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
