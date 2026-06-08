using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IItemImageRepository
    {
        Task Add(ItemImage model);
        Task AddRange(List<ItemImage> images);
        Task<List<ItemImage>> GetByItemId(int itemMasterId);
        Task<bool> ExistsByHash(string hash);
        Task<ItemImage> GetById(int id);
        Task Delete(ItemImage img);
        Task UpdateRange(List<ItemImage> images);
        Task DeleteByItemId(int itemMasterId);
        Task<bool> IsReferenced(int id);
    }
}
