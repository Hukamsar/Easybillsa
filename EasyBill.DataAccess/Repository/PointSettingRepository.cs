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
    public class PointSettingRepository : IPointSettingRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PointSettingRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PointSetting> Create(PointSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PointSetting>();
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

        public async Task Delete(PointSetting model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<PointSetting>();
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

        public async Task<IList<PointSetting>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PointSetting>();
                IList<PointSetting> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PointSetting> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PointSetting>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PointSetting> Update(PointSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PointSetting>();
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
