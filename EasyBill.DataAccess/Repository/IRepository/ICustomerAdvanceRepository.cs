using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICustomerAdvanceRepository
    {
        Task<IList<CustomerAdvance>> GetAll();
        Task<CustomerAdvance> Create(CustomerAdvance model);
        Task<CustomerAdvance> GetById(int? Id);
        Task<CustomerAdvance> Update(CustomerAdvance model);
        Task Delete(CustomerAdvance model);
        Task<IList<CustomerAdvance>> GetByCustomerId(int? CustomerId);
    }
}
