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
    public class TermConditionsRepository : ITermConditionsRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public TermConditionsRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<TermsConditions> Create(TermsConditions model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<TermsConditions>();
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

        public async Task Delete(TermsConditions model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<TermsConditions>();
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

        public async Task<IList<TermsConditions>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<TermsConditions>();
                IList<TermsConditions> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<TermsConditions> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<TermsConditions>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<TermsConditions> Update(TermsConditions model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<TermsConditions>();
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
        public async Task<bool> CheckDuplicateAsync(string Name, int id)
        {
            var repo = _unitofwork.GetRepository<TermsConditions>();

            if (string.IsNullOrWhiteSpace(Name))
                return false;

            string cleanedName = Name.Trim().ToLower();


            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.Name != null &&

                    x.Name.ToLower() == cleanedName &&
                    x.Id != id

                );

            return alreadyExists;
        }
    }
}
