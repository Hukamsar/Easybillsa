using AOne.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public interface IStockReturnRepository
    {
        Task<IList<StockReturn>> GetAll();
        Task<IList<StockReturn>> GetByCustomerId(int? customerId);
        Task<StockReturn> Create(StockReturn model);
        Task<StockReturn> GetById(int? Id);
        Task<StockReturn> Update(StockReturn model);
        Task Delete(StockReturn model);
        Task<List<SelectListItem>> GetCustomersWithReturns();
    }
}
