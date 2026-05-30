using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseSettingRepository
    {
        Task<IList<PurchaseSetting>> GetAll();
        Task<PurchaseSetting> Create(PurchaseSetting model);
        Task<PurchaseSetting> GetById(int? Id);
        Task<PurchaseSetting> GetByUserId(string? userid);
        Task<PurchaseSetting> Update(PurchaseSetting model);
        Task Delete(PurchaseSetting model);
    }
}
