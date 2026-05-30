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
    public class DesignationRepository : IDesignationRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public DesignationRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<Designation>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Designation>();
                IList<Designation> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Designation> Create(Designation model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Designation>();
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
        public async Task<Designation> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Designation>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Designation> Update(Designation model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Designation>();
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
        public async Task Delete(Designation model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Designation>();
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
        public async Task<bool> ExistName(string name, int id)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            try
            {
                var repository = _unitofwork.GetRepository<Designation>();
                return await repository.Query().AnyAsync(g => g.Name.ToLower() == name.Trim().ToLower() && g.Id != id);
            }
            catch
            {
                throw;
            }
        }
    }
}
