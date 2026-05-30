using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IPOWithAIRepository
    {
        Task<IList<POWithAI>> GetAll();
        Task<POWithAI> Create(POWithAI model);
        Task<POWithAI> GetById(int? Id);
        Task<POWithAI> Update(POWithAI model);
        Task Delete(POWithAI model);
    }
}
