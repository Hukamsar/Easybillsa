using EasyBill.Models.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseChallanRepository
    {
        Task<PurchaseChallan> Create(PurchaseChallan model);
        Task<IList<PurchaseChallan>> GetAll();
        Task<PurchaseChallan?> GetById(int? Id);
        Task<bool> ExistsByBillNoAsync(string? billNo, int? excludeId = null);
        Task<PurchaseChallan> Update(PurchaseChallan model);
        Task MarkConvertedAsync(int challanId, int purchaseId);
        Task Delete(PurchaseChallan model);
    }
}
