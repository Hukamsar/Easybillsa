using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IDivisionRepository
    {
        Task<IList<Division>> GetAll();
        Task<Division> Create(Division model);
        Task<Division> GetById(int? Id);
        Task<Division> GetByName(string? name);
        Task<Division> Update(Division model);
        Task Delete(Division model);
        Task<IList<Division>> GetByCompanyList(int? companyId);
    }
}
