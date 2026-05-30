using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly IUnitOfWork _unitOfWork;
        public SupplierRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IList<Supplier>> GetALL()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Supplier>();
                return await repository.Query().Include(x => x.AccountGroup).ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Supplier> Create(Supplier model)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Supplier>();
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
        public async Task<Supplier> GetBySupplierId(int? Id)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Supplier>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<Supplier> Update(Supplier model)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<Supplier>();
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
        public async Task Delete(Supplier model)
        {
            try
            {
                var assetGroupRepository = _unitOfWork.GetRepository<Supplier>();
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
        public async Task ItemAddRange(List<Supplier> suppliers)
        {
            try
            {
                var itemRepo = _unitOfWork.GetRepository<Supplier>();
                using (var transaction = itemRepo.BeginTransaction())
                {
                    if (suppliers != null && suppliers.Count > 0)
                    {
                        itemRepo.AddRange(suppliers);
                        await itemRepo.SaveChangesAsync();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CreateWithSupplierAsync failed: " + ex.Message);
            }
        }
    }
}
