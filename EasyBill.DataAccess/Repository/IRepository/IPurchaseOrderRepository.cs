using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPurchaseOrderRepository
    {
        Task<IList<PurchaseOrder>> GetAll();
        Task<IList<PurchaseOrder>> GetIncomingRequests(string targetTenantId);
        Task<PurchaseOrder> Create(PurchaseOrder model); 
        Task<PurchaseOrder> GetById(int? Id);
        Task<PurchaseOrder> GetIncomingRequestById(int id);
        Task<PurchaseOrder> Update(PurchaseOrder model);
        Task Delete(PurchaseOrder model);
        Task<IList<PurchaseOrder>> GetBySupplierId(int supplierId);
    }
}
