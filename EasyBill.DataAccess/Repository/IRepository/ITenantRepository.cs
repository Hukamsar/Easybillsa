using AOne.Models;
using AOne.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ITenantRepository
    {
        Task<IList<Tenant>> GetAll();
        Task<Tenant> Create(Tenant model);
        Task<Tenant> GetById(string? Id);
        Task<Tenant> Update(Tenant model);
        Task Delete(Tenant model);
        Task<Tenant> GetByTenantId(string? tenantId);
        Task<bool> ExistMobile(string? mobileNo);
        Task<bool> ExistEmail(string? email);
        Task<IList<ApplicationUsers>> GetAllUsers();
    }
}
