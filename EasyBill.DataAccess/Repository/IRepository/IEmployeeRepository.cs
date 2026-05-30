using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IEmployeeRepository
    {
        Task<IList<Employee>> GetAll();
        Task<Employee> Create(Employee model);
        Task<Employee> GetById(int? Id);
        Task<Employee> Update(Employee model);
        Task Delete(Employee model);
        Task<bool> ExistMobile(string? phoneNo, int id);
        Task<bool> ExistEmail(string? email, int id);
        Task<bool> CheckDuplicateAsync(string name);
        Task<IList<Employee>> GetEmployeeByDepartment(int? department);
    }
}
