using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IContraRepository
    {
        Task<IList<Contra>> GetAll();
        Task<Contra> Create(Contra model);
        Task<Contra> GetById(int? Id);
        Task<Contra> Update(Contra model);
        Task Delete(Contra model);
    }
}
