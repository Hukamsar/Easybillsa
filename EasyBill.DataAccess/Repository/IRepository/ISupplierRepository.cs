using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISupplierRepository
    {
        Task<IList<Supplier>> GetALL();     
        Task<Supplier> Create(Supplier model);
        Task<Supplier> GetBySupplierId(int? Id);
        Task<Supplier> Update(Supplier model);
        Task Delete(Supplier model);
        Task ItemAddRange(List<Supplier> suppliers);
    }
}
