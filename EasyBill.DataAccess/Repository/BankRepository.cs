using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class BankRepository : IBankRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public BankRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public async Task<Bank> Create(Bank model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Bank>();
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

        public async Task Delete(Bank model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Bank>();
                repository.Delete(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IList<Bank>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Bank>();
                IList<Bank> results = await repository.Query().Include(x => x.AccountGroup).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Bank> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Bank>();
                var result = await repository.Query().Include(x => x.AccountGroup).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Bank> Update(Bank model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Bank>();
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

        public async Task<bool> CheckDuplicateAsync(string BankName, int id)
        {
            var repo = _unitofwork.GetRepository<Bank>();

            if (string.IsNullOrWhiteSpace(BankName))
                return false;

            string cleanedName = BankName.Trim().ToLower();

            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.BankName != null &&
                    x.BankName.ToLower() == cleanedName &&
                    x.Id != id
                );

            return alreadyExists;
        }
    }
}
