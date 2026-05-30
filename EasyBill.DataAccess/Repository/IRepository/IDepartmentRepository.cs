using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IDepartmentRepository
    {
        Task<IList<Department>> GetAll();
        Task<Department> Create(Department model);
        Task<Department> GetById(int? Id);
        Task<Department> Update(Department model);
        Task Delete(Department model);
        Task<bool> ExistName(string name, int id);
    }
}
