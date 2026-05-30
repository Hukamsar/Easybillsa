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
    public class PharmacyDoctorRepository : IPharmacyDoctorRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public PharmacyDoctorRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<PharmacyDoctor> Create(PharmacyDoctor model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PharmacyDoctor>();
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

        public async Task Delete(PharmacyDoctor model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<PharmacyDoctor>();
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

        public async Task<IList<PharmacyDoctor>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<PharmacyDoctor>();
                IList<PharmacyDoctor> results = await repository.Query().ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<PharmacyDoctor> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PharmacyDoctor>();
                var result = await repository.Query().Where(l => l.Id == Id).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PharmacyDoctor> GetDoctorByMobileno(string? phoneno)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PharmacyDoctor>();
                var result = await repository.Query().Where(l => l.PhoneNo == phoneno).FirstOrDefaultAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<PharmacyDoctor> Update(PharmacyDoctor model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<PharmacyDoctor>();
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
