using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISubCategoryRepository
    {
        Task<IList<SubCategory>> GetAll();
        Task<SubCategory> Create(SubCategory model);
        Task<SubCategory> GetById(int? Id);
        Task<SubCategory> GetByName(string? name);
        Task<SubCategory> GetByNameAndCategory(string? name, int categoryId);
        Task<SubCategory> Update(SubCategory model);
        Task Delete(SubCategory model);
    }
}
