using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IInvoiceRepository
    {
        Task<IList<Invoice>> GetAll();
        Task<Invoice> Create(Invoice model);
        Task<Invoice> GetById(int? Id);
        Task<Invoice> Update(Invoice model);
        Task Delete(Invoice model);
    }
}
