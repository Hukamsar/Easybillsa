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
    public class BatchRepository : IBatchRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public BatchRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Batch> Create(Batch model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Batch>();
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
        public async Task<IList<Batch>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Batch>();
                IList<Batch> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
        public async Task<Batch> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Batch>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        } 
        public async Task<Batch> Update(Batch model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Batch>();
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
        public async Task Delete(Batch model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<Batch>();
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
    }
}
