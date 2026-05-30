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
    public class CountryRepository : ICountryRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public CountryRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Country> Create(Country model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Country>();
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

        public async Task Delete(Country model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Country>();
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

        public async Task<IList<Country>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Country>();
                IList<Country> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Country> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Country>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Country> Update(Country model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Country>();
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
        public async Task<Country> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<Country>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<bool> ExistName(string name, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            try
            {
                var repository = _unitofwork.GetRepository<Country>();
                return await repository.Query().AnyAsync(g => g.Name.ToLower() == name.Trim().ToLower() && g.Id != id);
            }
            catch
            {
                throw;
            }
        }
    }
}
