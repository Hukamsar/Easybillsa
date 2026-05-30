using AOne.Models.Entity;
using EasyBill.Models.Entity;
using EasyBill.Models.Model.Response;
using EasyBill.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IItemMasterRepository
    {
        Task<IList<ItemMaster>> GetAll();
        Task<ItemMaster> Create(ItemMaster model);
        Task ItemAddRange(List<ItemMaster> items);
        Task<ItemMaster> GetByItemMasterId(int? Id);
        Task<ItemMaster> GetByBarcode(string? barcode);
        Task<ItemMaster> Update(ItemMaster model);
        Task Delete(ItemMaster model);
        Task<bool> ExistsByCode(string code);
        //Task<bool> CheckDuplicateAsync(string Code, int id);

        Task<int> HasAnySaleAsync(int id, string tenantId);


        Task<IList<CategoryItems>> GetAllCategoryItems();
        Task<IList<ItemMaster>> GetItemsByCategory(int categoryId);
        Task<IList<ItemSearchDto>> GetItemsBySearch(string query);
        //Task<List<object>> GetListOfOrderByCustomer(int customerId);
        Task<List<OrderResponse>> GetOrdersByCustomer(int customerId);

        Task<ItemMaster> GetProductById(int Id);
    }
}
