using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class CategoryMasterRepository : ICategoryMasterRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public CategoryMasterRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<CategoryMaster>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<CategoryMaster>();
                IList<CategoryMaster> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<CategoryMaster> Create(CategoryMaster model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CategoryMaster>();
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
        public async Task<CategoryMaster> GetByCategoryMasterId(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CategoryMaster>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    
        public async Task<CategoryMaster> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<CategoryMaster>();

                var result = await repository.Query().Where(l => l.CategoryName != null && l.CategoryName.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }

        public async Task<CategoryMaster> Update(CategoryMaster model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<CategoryMaster>();
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
        public async Task Delete(CategoryMaster model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<CategoryMaster>();
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
