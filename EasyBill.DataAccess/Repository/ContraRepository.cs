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
    public class ContraRepository : IContraRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public ContraRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public async Task<Contra> Create(Contra model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Contra>();
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

        public async Task Delete(Contra model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Contra>();
                repository.Delete(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<IList<Contra>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Contra>();
                IList<Contra> results = await repository.Query().Include(x => x.Bank).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Contra> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Contra>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Contra> Update(Contra model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Contra>();
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
    }
}
