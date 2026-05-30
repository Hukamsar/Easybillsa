using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IAccountGroupRepository
    {
        Task<IList<AccountGroup>> GetAll();
        Task<AccountGroup> Create(AccountGroup model);
        Task<AccountGroup> GetById(int? Id);
        Task<AccountGroup> Update(AccountGroup model);
        Task Delete(AccountGroup model);
        Task<AccountGroup> GetByName(string? name);
        Task<bool> CheckDuplicateAsync(string name, int id);    }
}
