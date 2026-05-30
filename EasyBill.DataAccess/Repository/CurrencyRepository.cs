using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace EasyBill.DataAccess.Repository
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        public CurrencyRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IList<Currency>> GetAll()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Currency>();
                return await repository.Query().ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Currency> Create(Currency model)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Currency>();
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

        public async Task<Currency> GetById(int id)
        {
            return await _unitOfWork
                .GetRepository<Currency>()
                .Query()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Currency> Update(Currency model)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Currency>();
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
        public async Task Delete(Currency model)
        {
            try
            {
                var assetGroupRepository = _unitOfWork.GetRepository<Currency>();
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
        public async Task<Currency> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitOfWork.GetRepository<Currency>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
    }
}
