using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseItemRepository
    {
        Task<IList<PurchaseItem>> GetAll();
        Task<IList<PurchaseItem>> GetByItemMasterId(int itemmasterId);
        Task<PurchaseItem> Create(PurchaseItem model);
        Task<PurchaseItem> GetById(int? Id);
        Task<PurchaseItem> Update(PurchaseItem model);
        Task Delete(PurchaseItem model);
    }
}
