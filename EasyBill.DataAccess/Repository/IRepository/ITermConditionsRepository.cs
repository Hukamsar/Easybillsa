using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ITermConditionsRepository
    {
        Task<IList<TermsConditions>> GetAll();
        Task<TermsConditions> Create(TermsConditions model);
        Task<TermsConditions> GetById(int? Id);
        Task<TermsConditions> Update(TermsConditions model);
        Task Delete(TermsConditions model);
        Task<bool> CheckDuplicateAsync(string Name, int id);
    }
}
