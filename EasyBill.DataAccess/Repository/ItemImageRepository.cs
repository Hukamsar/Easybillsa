using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace EasyBill.DataAccess.Repository
{
    public class ItemImageRepository : IItemImageRepository
    {
        private readonly IUnitOfWork _unitOfWork;

        public ItemImageRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Add(ItemImage model)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            repo.Add(model);

            using (var transaction = repo.BeginTransaction())
            {
                await repo.SaveChangesAsync();
                transaction.Commit();
            }
        }

        public async Task AddRange(List<ItemImage> images)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            repo.AddRange(images);
            using (var transaction = repo.BeginTransaction())
            { 
                await repo.SaveChangesAsync();
                transaction.Commit();
            }
        }

        public async Task<List<ItemImage>> GetByItemId(int itemMasterId)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();

            return await repo.Query()
                .Where(x => x.ItemMasterId == itemMasterId && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        public async Task<bool> ExistsByHash(string hash)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            return await repo.Query().AnyAsync(x => x.ImageHash == hash);
        }

        public async Task<ItemImage> GetById(int id)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            return await repo.Query()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task Delete(ItemImage img)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();

            var existing = await repo.Query()
                .FirstOrDefaultAsync(x => x.Id == img.Id);

            if (existing != null)
            {
                repo.Delete(existing);
            }
        }

        public async Task UpdateRange(List<ItemImage> images)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            repo.UpdateRange(images);
            await repo.SaveChangesAsync();
        }
        public async Task DeleteByItemId(int itemMasterId)
        {
            var repo = _unitOfWork.GetRepository<ItemImage>();
            repo.DeleteWhere(x => x.ItemMasterId == itemMasterId);
        }
        public Task<bool> IsReferenced(int id)
        {
            return _unitOfWork.IsRecordReferencedAsync<ItemMaster>(id);
        }
    }
}