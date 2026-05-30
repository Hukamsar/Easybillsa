using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseReturnRepository
    {
        Task<IList<PurchaseReturn>> GetAll();
        Task<PurchaseReturn> Create(PurchaseReturn model);
        Task CreateRange(List<PurchaseReturn> purchasereturn); 
        Task<PurchaseReturn> GetById(int? Id);
        Task<IList<PurchaseReturn>> GetBySupplierId(int supplierId);
        Task<PurchaseReturn> Update(PurchaseReturn model);
        Task Delete(PurchaseReturn model);
    }
}
