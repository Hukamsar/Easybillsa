using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IOpeningStockRepository
    {
        Task<IList<OpeningStock>> GetAll();
        Task<OpeningStock> Create(OpeningStock model);
        Task<OpeningStock> GetById(int? Id);
        Task<OpeningStock> Update(OpeningStock model);
        Task Delete(OpeningStock model);
        Task AddRange(List<OpeningStock> items);
    }
}
