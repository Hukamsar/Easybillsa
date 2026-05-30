using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICountryRepository
    {
        Task<IList<Country>> GetAll();
        Task<Country> Create(Country model);
        Task<Country> GetById(int? Id);
        Task<Country> Update(Country model);
        Task Delete(Country model);
        Task<Country> GetByName(string? name);
        Task<bool> ExistName(string name, int id);
    }
}
