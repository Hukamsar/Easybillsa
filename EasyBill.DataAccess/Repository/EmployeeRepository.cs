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
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public EmployeeRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Employee>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
                IList<Employee> results = await repository.Query()
                    .Where(x => !x.IsHoAdmin)
                    .Include(x => x.Designation)
                    .Include(x => x.Departments)
                    .ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Employee> Create(Employee model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
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
        public async Task<Employee> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Employee> Update(Employee model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
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
        public async Task Delete(Employee model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Employee>();
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
        public async Task<bool> ExistMobile(string? phoneNo, int id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
                var result = await repository.Query().AnyAsync(x => x.Phone != null && phoneNo != null && x.Phone.Trim().ToLower() == phoneNo.Trim().ToLower() && x.Id != id);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> ExistEmail(string? email, int id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
                var result = await repository.Query().AnyAsync(x => x.Email != null && email != null && x.Email.Trim().ToLower() == email.Trim().ToLower() && x.Id != id);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> CheckDuplicateAsync(string name)
        {
            var repo = _unitofwork.GetRepository<Employee>();

            if (string.IsNullOrWhiteSpace(name))
                return false;

            string cleanedName = name.Trim().ToLower();

            bool alreadyExists = await repo.Query().AnyAsync(x => x.Name != null && x.Name.ToLower() == cleanedName);

            return alreadyExists;
        }
        public async Task<IList<Employee>> GetEmployeeByDepartment(int? departmentId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Employee>();
                IList<Employee> results = await repository.Query().Where(x => x.DepartMentId == departmentId).Include(x => x.Designation).Include(x => x.Departments).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
