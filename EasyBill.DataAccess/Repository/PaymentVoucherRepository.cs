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
    public class PaymentVoucherRepository : IpaymentVoucherRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PaymentVoucherRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PaymentVoucher> Create(PaymentVoucher model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucher>();
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

        public async Task Delete(PaymentVoucher model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<PaymentVoucher>();
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

        public async Task<IList<PaymentVoucher>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucher>();
                IList<PaymentVoucher> results = await repository.Query().Include(x => x.Suppliers).Include(x => x.paymentVouchercategory).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaymentVoucher> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucher>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PaymentVoucher> Update(PaymentVoucher model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PaymentVoucher>();
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
