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
    public class SubCategoryRepository : ISubCategoryRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public SubCategoryRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<SubCategory>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<SubCategory>();
                IList<SubCategory> results = await repository.Query().Include(x => x.Category).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SubCategory> Create(SubCategory model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SubCategory>();
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
        public async Task<SubCategory> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SubCategory>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<SubCategory?> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<SubCategory>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower()).FirstOrDefaultAsync();

                return result;
            }
            catch (Exception)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<SubCategory?> GetByNameAndCategory(string? name, int categoryId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<SubCategory>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower().Trim() == name.ToLower().Trim() && l.CategoryId == categoryId).FirstOrDefaultAsync();

                return result;
            }
            catch (Exception)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<SubCategory> Update(SubCategory model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SubCategory>();
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
        public async Task Delete(SubCategory model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<SubCategory>();
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
