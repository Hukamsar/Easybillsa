using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IStockRepository
    {
        Task<IList<Stock>> GetAll();
        Task<Stock> Create(Stock model);
        Task<Stock> GetById(int? Id);
        Task<Stock> Update(Stock model);
        Task CreateWithStockAsync(List<Stock> stocks);
        Task UpdateRangeStocksAsync(List<Stock> stocks);
        Task Delete(Stock model);
    }
}
