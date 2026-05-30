using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ICityRepository
    {
        Task<IList<City>> GetAll();
        Task<City> Create(City model);
        Task<City> GetById(int? Id);
        Task<City> Update(City model);
        Task Delete(City model);
        Task<IList<City>> GetByStateId(int StateId);
        Task<City> GetByName(string? name);
        Task<bool> CheckDuplicateAsync(string name, int stateId, int id);
        
        }
}
