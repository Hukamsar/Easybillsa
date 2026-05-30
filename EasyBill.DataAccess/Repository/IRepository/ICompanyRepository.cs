using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICompanyRepository
    {
        Task<IList<Company>> GetAll();
        Task<Company> Create(Company model);
        Task<Company> GetById(int? Id);
        Task<Company> GetByName(string? name);
        Task<Company> Update(Company model);
        Task Delete(Company model);
    }
}
