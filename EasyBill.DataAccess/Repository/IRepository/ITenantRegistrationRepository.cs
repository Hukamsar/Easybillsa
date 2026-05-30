using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.Entity;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ITenantRegistrationRepository
    {
        Task<IList<Tenant>> GetAll();
        Task<Tenant> Create(Tenant model);
        Task<Tenant> GetById(string? Id);
        Task<Tenant> Update(Tenant model);
        Task Delete(Tenant model);
        Task<bool> CheckDuplicateAsync(string? Name, string? GstNo, string? MobileNo, string? Email, string? id);
    }
}
