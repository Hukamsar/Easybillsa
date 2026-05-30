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
    public class AccountGroupRepository : IAccountGroupRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public AccountGroupRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<AccountGroup> Create(AccountGroup model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<AccountGroup>();
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
        public async Task<IList<AccountGroup>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<AccountGroup>();
                IList<AccountGroup> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<AccountGroup> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<AccountGroup>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<AccountGroup> Update(AccountGroup model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<AccountGroup>();
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
        public async Task Delete(AccountGroup model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<AccountGroup>();
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
        public async Task<AccountGroup> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<AccountGroup>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<bool> CheckDuplicateAsync(string name, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string cleanedName = name.Trim().ToLower();

            var repo = _unitofwork.GetRepository<AccountGroup>();

            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.Name != null &&
                    x.Name.Trim().ToLower() == cleanedName &&
                    x.Id != id
                );

            return alreadyExists;
        }
    }
}