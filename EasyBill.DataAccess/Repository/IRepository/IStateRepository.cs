using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBill.Models.Entity;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IStateRepository
    {
        Task<IList<State>> GetAll();
        Task<State> Create(State model);
        Task<State> GetById(int? Id);
        Task<State> Update(State model);
        Task Delete(State model);
        Task<IList<State>> GetByCountryId(int CountryId);
        Task<State> GetByName(string? name);
        Task<bool> CheckDuplicateAsync(string name, int countryId, int id);        }
    }
