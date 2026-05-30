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
    public class SalsePaymentDetailsRepository : ISalsePaymentDetailsRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SalsePaymentDetailsRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<SalsePaymentDetails> Create(SalsePaymentDetails model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalsePaymentDetails>();
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

        public async Task Delete(SalsePaymentDetails model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<SalsePaymentDetails>();
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

        public async Task<IList<SalsePaymentDetails>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalsePaymentDetails>();
                IList<SalsePaymentDetails> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SalsePaymentDetails> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalsePaymentDetails>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SalsePaymentDetails> Update(SalsePaymentDetails model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalsePaymentDetails>();
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
    }
}
