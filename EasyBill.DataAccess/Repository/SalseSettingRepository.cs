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
    public class SalseSettingRepository : ISalseSettingRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SalseSettingRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<SalseSetting> Create(SalseSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalseSetting>();
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

        public async Task Delete(SalseSetting model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<SalseSetting>();
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

        public async Task<IList<SalseSetting>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalseSetting>();
                IList<SalseSetting> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<SalseSetting> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalseSetting>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SalseSetting> GetByUserId(string? userId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalseSetting>();
                var result = await repository.Query().Where(l => l.ApplicationUserId == userId).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SalseSetting> Update(SalseSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalseSetting>();
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
