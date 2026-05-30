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
    public class OfferRepository : IOfferRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public OfferRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }
        public async Task<Offer> Create(Offer model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Offer>();
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

        public async Task Delete(Offer model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<Offer>();
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

        public async Task<IList<Offer>> GetAll()
        {
            try
            {
                var repository = _unitofwork.GetRepository<Offer>();
                IList<Offer> results = await repository.Query().Include(x => x.OfferItems).ToListAsync();
                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Offer> GetById(int? Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Offer>();
                var result = await repository.Query().Include(x => x.OfferItems).ThenInclude(x => x.FreeItem).Include(x => x.OfferItems).ThenInclude(x => x.ItemMaster).Where(l => l.Id == Id).FirstOrDefaultAsync(); 
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Offer> Update(Offer model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<Offer>();
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
        public async Task<bool> CheckDuplicateAsync(string OfferName, int id)
        {
            var repo = _unitofwork.GetRepository<Offer>();

            if (string.IsNullOrWhiteSpace(OfferName))
                return false;

            string cleanedName = OfferName.Trim().ToLower();


            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.OfferName != null &&

                    x.OfferName.ToLower() == cleanedName &&
                    x.Id != id

                );

            return alreadyExists;
        }
    }


}

