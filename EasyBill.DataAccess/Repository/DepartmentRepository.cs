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
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public DepartmentRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Department>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Department>();
                IList<Department> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Department> Create(Department model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Department>();
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
        public async Task<Department> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Department>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Department> Update(Department model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Department>();
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
        public async Task Delete(Department model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Department>();
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
        public async Task<bool> ExistName(string name, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            try
            {
                var repository = _unitofwork.GetRepository<Department>();
                return await repository.Query().AnyAsync(g => g.Name.ToLower() == name.Trim().ToLower() && g.Id != id);
            }
            catch
            {
                throw;
            }
        }
    }
}
