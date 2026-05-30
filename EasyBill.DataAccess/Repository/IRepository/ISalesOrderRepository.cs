using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISalesOrderRepository
    {
        Task<IList<SalesOrder>> GetAll();
        Task<SalesOrder> Create(SalesOrder model);
        Task<SalesOrder> GetById(int? Id);
        Task<IList<SalesOrder>> GetByCustomerId(int? customerId);
        Task<SalesOrder> Update(SalesOrder model);
        Task Delete(SalesOrder model);

        Task<IList<SalesOrder>> GetByCustomerIdWithPayments(int? customerId);
    }
}
