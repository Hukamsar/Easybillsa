using AOne.DataAccess.Repository.IRepository;
using AOne.Models;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class ClinicRepository : ITenantRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public ClinicRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Tenant> Create(Tenant model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
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


        public async Task<IList<Tenant>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                IList<Tenant> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Tenant> GetById(string? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                var result = await repository.Query()
                    .Include(x => x.SubscriptionPlan)
                    .Where(x => x.Id == Id)
                    .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Tenant> Update(Tenant model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
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
        public async Task Delete(Tenant model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<Tenant>();
                batchRepository.Delete(model);
                using (var transaction = batchRepository.BeginTransaction())
                {
                    await batchRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Tenant> GetByTenantId(string? tenantId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                var result = await repository.Query()
                    .Include(x => x.SubscriptionPlan)
                    .Where(x => x.Id == tenantId)
                    .FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> ExistMobile(string? mobileNo)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                var result = await repository.Query().AnyAsync(x => x.MobileNo == mobileNo);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> ExistEmail(string? email)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                var result = await repository.Query().AnyAsync(x => x.Email == email);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<ApplicationUsers>> GetAllUsers()
        {
            try
            {
                var repository = _unitofwork.GetRepository<ApplicationUsers>();
                IList<ApplicationUsers> results = await repository.Query().Include(x => x.Employee).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
