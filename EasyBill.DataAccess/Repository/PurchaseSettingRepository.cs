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
    public class PurchaseSettingRepository : IPurchaseSettingRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PurchaseSettingRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PurchaseSetting> Create(PurchaseSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseSetting>();
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

        public async Task Delete(PurchaseSetting model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<PurchaseSetting>();
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

        public async Task<IList<PurchaseSetting>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseSetting>();
                IList<PurchaseSetting> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PurchaseSetting> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseSetting>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PurchaseSetting> GetByUserId(string? userId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseSetting>();
                var result = await repository.Query().Where(l => l.ApplicationUserId == userId).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PurchaseSetting> Update(PurchaseSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PurchaseSetting>();
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
