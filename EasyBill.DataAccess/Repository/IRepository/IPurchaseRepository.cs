using EasyBill.Models.Entity;
using EasyBill.Models.Model.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseRepository
    {
        Task<IList<Purchase>> GetAll();
        Task<Purchase> Create(Purchase model);
        Task<Purchase> CreateWithStockAsync(Purchase model, List<Stock> stocks);

        Task<Purchase> GetById(int? Id);
        Task<Purchase> Update(Purchase model);
        Task Delete(Purchase model);
        Task<IList<Purchase>> GetBySupplierId(int supplierId);
        Task<IList<Purchase>> GetPendingBillsBySupplierId(int? supplierid);
        Task<List<ItemPurchaseHistoryDto>> GetItemSupplierPurchaseInfoByItemIdAsync(int itemId);
        Task<List<ItemPurchaseHistoryDto>> GetItemPurchaseHistory(int itemId, int supplierId);
        Task<bool> IsBillNoDuplicateAsync(string billNo, int id = 0);

    }
}
