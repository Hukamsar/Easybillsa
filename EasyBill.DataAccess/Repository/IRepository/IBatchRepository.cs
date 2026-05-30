using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IBatchRepository
    {
        Task<IList<Batch>> GetAll();
        Task<Batch> Create(Batch model);
        Task<Batch> GetById(int? Id);
        Task<Batch> Update(Batch model);
        Task Delete(Batch model);
    }
}
