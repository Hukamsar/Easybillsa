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
    public class ModeOfPaymentRepository : IModeOfPaymentRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public ModeOfPaymentRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<ModeOfPayment> Create(ModeOfPayment model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ModeOfPayment>();
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

        public async Task Delete(ModeOfPayment model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<ModeOfPayment>();
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

        public async Task<IList<ModeOfPayment>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<ModeOfPayment>();
                IList<ModeOfPayment> results = await repository.Query().Include(x => x.Bank).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ModeOfPayment> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ModeOfPayment>();
                var result = await repository.Query().Include(x => x.Bank).Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<ModeOfPayment> Update(ModeOfPayment model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ModeOfPayment>();
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
            var repo = _unitofwork.GetRepository<ModeOfPayment>();

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
