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
    public class POWithAIRepository : IPOWithAIRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public POWithAIRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<POWithAI>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<POWithAI>();
                IList<POWithAI> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<POWithAI> Create(POWithAI model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<POWithAI>();
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
        public async Task<POWithAI> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<POWithAI>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<POWithAI> Update(POWithAI model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<POWithAI>();
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
        public async Task Delete(POWithAI model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<POWithAI>();
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
         
    }
}
