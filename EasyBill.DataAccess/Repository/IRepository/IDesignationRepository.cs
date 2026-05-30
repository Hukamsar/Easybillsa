using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IDesignationRepository
    {
        Task<IList<Designation>> GetAll();
        Task<Designation> Create(Designation model);
        Task<Designation> GetById(int? Id);
        Task<Designation> Update(Designation model);
        Task Delete(Designation model);
        Task<bool> ExistName(string name, int id);
    }
}
