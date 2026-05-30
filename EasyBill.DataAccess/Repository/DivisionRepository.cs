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
    public class DivisionRepository : IDivisionRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public DivisionRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Division>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Division>();
                IList<Division> results = await repository.Query().Include(x => x.Company).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Division> Create(Division model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Division>();
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
        public async Task<Division> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Division>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Division> GetByName(string? name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                var repository = _unitofwork.GetRepository<Division>();

                var result = await repository.Query().Where(l => l.Name != null && l.Name.ToLower() == name.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<Division> Update(Division model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Division>();
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
        public async Task Delete(Division model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Division>();
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
        public async Task<IList<Division>> GetByCompanyList(int? companyId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Division>();
                IList<Division> results = await repository.Query().Where(x => x.CompanyId == companyId).Include(x => x.Company).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
    }
}
