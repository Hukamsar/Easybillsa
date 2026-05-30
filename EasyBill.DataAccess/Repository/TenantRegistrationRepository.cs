using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class TenantRegistrationRepository : ITenantRegistrationRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public TenantRegistrationRepository(IUnitOfWork unitofwork)
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

        public async Task Delete(Tenant model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Tenant>();
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

        public async Task<IList<Tenant>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Tenant>();
                IList<Tenant> results = await repository.Query().Include(x => x.State).ToListAsync();
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
                var result = await repository.Query().Include(x => x.Country).Include(x => x.State).Include(x => x.City).Include(x => x.SubscriptionPlan).Where(l => l.Id == Id).FirstOrDefaultAsync();
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
        public async Task<bool> CheckDuplicateAsync(string? Name, string? GstNo, string? MobileNo, string? Email, string? id)
        {
            var repo = _unitofwork.GetRepository<Tenant>();

            string cleanedName = Name?.Trim().ToLower();
            string cleanedGst = GstNo?.Trim().ToLower();
            string cleanedMobile = MobileNo?.Trim();
            string cleanedemail = Email?.Trim();

            return await repo.Query().AnyAsync(x =>

                (
                    (!string.IsNullOrEmpty(cleanedName) && x.Name != null && x.Name.ToLower() == cleanedName)
                    ||
                    (!string.IsNullOrEmpty(cleanedGst) && x.GstNo != null && x.GstNo.ToLower() == cleanedGst)
                    ||
                    (!string.IsNullOrEmpty(cleanedMobile) && x.MobileNo != null && x.MobileNo == cleanedMobile)
                    ||
                    (!string.IsNullOrEmpty(cleanedemail) && x.Email != null && x.Email == cleanedemail)
                )

                && x.Id != id   
            );
        }

    }
}

