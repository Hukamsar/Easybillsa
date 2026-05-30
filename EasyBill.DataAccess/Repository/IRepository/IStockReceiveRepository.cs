using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IStockReceiveRepository
    {
        Task<IList<StockReceive>> GetAll();
        Task<StockReceive> Create(StockReceive model);
        Task<StockReceive> GetById(int? Id);
        Task<StockReceive> Update(StockReceive model);
        Task Delete(StockReceive model);
    }
}
