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
    public class OpeningStockRepository : IOpeningStockRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public OpeningStockRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<OpeningStock> Create(OpeningStock model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<OpeningStock>();
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

        public async Task Delete(OpeningStock model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<OpeningStock>();
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

        public async Task<IList<OpeningStock>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<OpeningStock>();
                IList<OpeningStock> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<OpeningStock> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<OpeningStock>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<OpeningStock> Update(OpeningStock model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<OpeningStock>();
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
        public async Task AddRange(List<OpeningStock> items)
        {
            try
            {
                var itemRepo = _unitofwork.GetRepository<OpeningStock>();
                using (var transaction = itemRepo.BeginTransaction())
                {
                    if (items != null && items.Count > 0)
                    {
                        itemRepo.AddRange(items);
                        await itemRepo.SaveChangesAsync();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CreateWithOpeningStockAsync failed: " + ex.Message);
            }
        }
    }
}
