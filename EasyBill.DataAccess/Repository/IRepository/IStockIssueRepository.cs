using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IStockIssueRepository
    {
        Task<IList<StockIssue>> GetAll();
        Task<IList<StockIssue>> GetPendingTransfersForBranch(string tenantId);
        Task<StockIssue> Create(StockIssue model);
        Task<StockIssue> GetById(int? Id);
        Task<StockIssue> GetByIdBypassTenant(int id);
        Task<StockIssue> Update(StockIssue model);
        Task Delete(StockIssue model);
    }
}
