using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IHSNRepository
    {
        Task<IList<Hsn>> GetAll();
        Task<Hsn> Create(Hsn model);
        Task<Hsn> GetByHSNId(int? Id);
        Task<Hsn> GetByCode(string? hsncode);
        Task<Hsn> Update(Hsn model);
        Task Delete(Hsn model);
        Task<bool> CheckDuplicateAsync(string HsnCode, int id);
    }
        
        
}
