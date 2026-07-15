using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IBankRepository
    {
        Task<IList<Bank>> GetAll();
        Task<Bank> Create(Bank model);
        Task<Bank> GetById(int? Id);
        Task<Bank> Update(Bank model);
        Task Delete(Bank model);
        Task<bool> CheckDuplicateAsync(string BankName, int id);
    }
}
