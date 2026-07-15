using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface ISalesRepository
    {
        Task<IList<Sales>> GetAll();
        Task<IList<Sales>> GetAllGSTR1Hsn();
        Task<Sales> Create(Sales model);
        Task<Sales> GetById(int? Id);
        Task<IList<Sales>> GetByCustomerId(int? customerId);
        Task<Sales> Update(Sales model);
        Task Delete(Sales model);
        Task<IList<Sales>> GetPendingBillsByCustomerId(int? customerId);

        Task<IList<HoldSalesListVM>> GetAllHoldSales();
        Task<HoldSales> GetHoldSalesById(int id);
        Task<HoldSales> CreateHold(HoldSales model);
        Task UpdateHold(HoldSales model);
        Task DeleteHoldSales(int holdId); 

        Task<IList<Sales>> GetAllGSTR1Data();
        Task<IList<Sales>> GetB2CLargeInvoices(string tenantId, DateTime? startDate, DateTime? endDate);
        Task<IList<Sales>> GetB2CSmallInvoices(string tenantId, DateTime? startDate, DateTime? endDate);
        Task<IList<Sales>> GetDeletedSales();
        Task<bool> RestoreSales(int id);
    }
}
