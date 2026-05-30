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

namespace EasyBill.DataAccess.Repository
{
    public class CityRepository : ICityRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public CityRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<City> Create(City model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<City>();
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

        public async Task Delete(City model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<City>();
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

        public async Task<IList<City>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<City>();
                IList<City> results = await repository.Query().Include(x => x.State).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<City> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<City>();
                var result = await repository.Query().Include(x => x.State).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<IList<City>> GetByStateId(int StateId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<City>();
                IList<City> results = await repository.Query().Include(x => x.Country).Include(x => x.State).Include(x => x.State).Where(x => x.StateId == StateId).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<City> Update(City model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<City>();
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
        public async Task<City> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<City>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<bool> CheckDuplicateAsync(string name, int stateId, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var cleanedName = name.Trim().ToLower();

            var repo = _unitofwork.GetRepository<City>(); // ✅ City entity

            return await repo.Query().AnyAsync(x =>
                x.Id != id &&                         // ✅ current record ignore
                x.StateId == stateId &&               // ✅ same state
                x.Name != null &&
                x.Name.Trim().ToLower() == cleanedName // ✅ exact match (case-insensitive)
            );
        }
    }
}
