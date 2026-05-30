using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class StateRepository : IStateRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public StateRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<State> Create(State model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<State>();
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

        public async Task Delete(State model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<State>();
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

        public async Task<IList<State>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<State>();
                IList<State> results = await repository.Query().Include(x => x.Country).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<State> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<State>();
                var result = await repository.Query().Include(x => x.Country).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<State>> GetByCountryId(int countryId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<State>();
                IList<State> results = await repository.Query().Where(x => x.CountryId == countryId).Include(x => x.Country).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<State> Update(State model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<State>();
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
        public async Task<State> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<State>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<bool> CheckDuplicateAsync(string name, int countryId, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            string cleaned = name.Trim().ToLower();

            var repo = _unitofwork.GetRepository<State>(); // ✅ State entity

            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.Id != id &&                          // ✅ current record ignore
                    x.CountryId == countryId &&            // ✅ same country
                    x.Name != null &&
                    x.Name.Trim().ToLower() == cleaned     // ✅ same name (case-insensitive)
                );

            return alreadyExists;
        }
    }
}