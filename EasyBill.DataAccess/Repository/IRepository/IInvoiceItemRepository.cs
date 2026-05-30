using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IInvoiceItemRepository
    {
        Task<IList<InvoiceItem>> GetAll();
        Task<InvoiceItem> Create(InvoiceItem model);
        Task<InvoiceItem> GetById(int? Id);
        Task<InvoiceItem> Update(InvoiceItem model);
        Task Delete(InvoiceItem model);
    }
}
