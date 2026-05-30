using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class HSNRepository : IHSNRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public HSNRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Hsn>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Hsn>();
                IList<Hsn> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Hsn> Create(Hsn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Hsn>();
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
        public async Task<Hsn> GetByHSNId(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Hsn>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        public async Task<Hsn> GetByCode(string? hsncode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hsncode))
                    return null;

                var repository = _unitofwork.GetRepository<Hsn>();

                var result = await repository.Query().Where(l => l.HsnCode != null && l.HsnCode.ToLower() == hsncode.ToLower())
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw; // no need to re-throw ex manually
            }
        }
        public async Task<Hsn> Update(Hsn model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Hsn>();
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
        public async Task Delete(Hsn model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Hsn>();
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
        public async Task<bool> CheckDuplicateAsync(string HsnCode, int id)
        {
            var repo = _unitofwork.GetRepository<Hsn>();

            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.HsnCode == HsnCode &&
                    x.Id != id
                );

            return alreadyExists;
        }
    }
}



