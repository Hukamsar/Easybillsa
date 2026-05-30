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
    public class PaymentVoucherCategoryRepository : IPaymentVoucherCategoryRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PaymentVoucherCategoryRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PaymentVoucherCategory> Create(PaymentVoucherCategory model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucherCategory>();
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

        public async Task Delete(PaymentVoucherCategory model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<PaymentVoucherCategory>();
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

        public async Task<IList<PaymentVoucherCategory>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucherCategory>();
                IList<PaymentVoucherCategory> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaymentVoucherCategory> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucherCategory>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaymentVoucherCategory> Update(PaymentVoucherCategory model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucherCategory>();
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
