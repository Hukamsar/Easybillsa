using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOne.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICategoryMasterRepository
    {
        Task<IList<CategoryMaster>> GetAll();
        Task<CategoryMaster> Create(CategoryMaster model);
        Task<CategoryMaster> GetByCategoryMasterId(int? Id);
        Task<CategoryMaster> Update(CategoryMaster model);
        Task<CategoryMaster> GetByName(string? name);
        Task Delete(CategoryMaster model);
    }
}
