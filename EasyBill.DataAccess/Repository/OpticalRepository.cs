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
    public class OpticalRepository : IOpticalRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public OpticalRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Optical> Create(Optical model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Optical>();
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
        public async Task<IList<Optical>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Optical>();
                IList<Optical> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Optical> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Optical>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Optical> Update(Optical model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Optical>();
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
        public async Task Delete(Optical model)
        {
            try
            {
                var batchRepository = _unitofwork.GetRepository<Optical>();
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
        public async Task<IList<Optical>> GetBySalesOrder(int salesOrderId, int itemmasterId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Optical>();
                IList<Optical> results = await repository.Query().Where(x => x.SalesOrderId == salesOrderId && x.ItemMasterId == itemmasterId).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
