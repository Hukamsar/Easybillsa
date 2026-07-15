using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EasyBill.DataAccess.Repository
{
    public class OfferRepository : IOfferRepository
    {
        private readonly IUnitOfWork _unitofwork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public OfferRepository(IUnitOfWork unitofwork, IHttpContextAccessor httpContextAccessor)
        {
            _unitofwork = unitofwork;
            _httpContextAccessor = httpContextAccessor;
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
                var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
                var repository = _unitofwork.GetRepository<Offer>();

                if (string.IsNullOrEmpty(tenantId))
                {
                    // For HO / SuperAdmin: Return all offers
                    return await repository.GetAll().Include(x => x.OfferItems).ToListAsync();
                }

                // For Branch: Get mapped offers explicitly bypassing CreatedBy filter
                var mappedOfferIds = await _unitofwork.GetRepository<EasyBill.Models.Entity.OfferStoreMapping>()
                                        .GetAll()
                                        .Where(m => m.TenantId == tenantId)
                                        .Select(m => m.OfferId)
                                        .ToListAsync();

                var results = await repository.GetAll()
                                .Include(x => x.OfferItems)
                                .Where(x => x.TenantId == tenantId || mappedOfferIds.Contains(x.Id))
                                .ToListAsync();

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

