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
    public class ReceiveVoucherRepository : IReceiveVoucherRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public ReceiveVoucherRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<ReceiveVoucher>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<ReceiveVoucher>();
                IList<ReceiveVoucher> results = await repository.Query().Include(x => x.PaymentCategory).Include(x => x.Customers).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<ReceiveVoucher> Create(ReceiveVoucher model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ReceiveVoucher>();
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
        public async Task<ReceiveVoucher> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ReceiveVoucher>();
                var result = await repository.Query().Include(x => x.PaymentCategory).Include(x => x.Customers).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<ReceiveVoucher> Update(ReceiveVoucher model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ReceiveVoucher>();
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
      
        public async Task Delete(ReceiveVoucher model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<ReceiveVoucher>();
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
