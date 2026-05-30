using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICurrencyRepository
    {
        Task<IList<Currency>> GetAll();
        Task<Currency> Create(Currency model);
        Task<Currency> GetById(int id);
        Task<Currency> Update(Currency model);
        Task Delete(Currency model);
        Task<Currency> GetByName(string? name);
    }
}
