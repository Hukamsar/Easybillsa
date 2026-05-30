using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISupplierAdvanceRepository
    {
        Task<IList<SupplierAdvance>> GetAll();
        Task<SupplierAdvance> Create(SupplierAdvance model);
        Task<SupplierAdvance> GetById(int? Id);
        Task<SupplierAdvance> Update(SupplierAdvance model);
        Task Delete(SupplierAdvance model);
        Task<IList<SupplierAdvance>> GetBySupplierId(int? supplierId);
    }
}
