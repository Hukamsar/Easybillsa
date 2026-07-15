using EasyBill.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IStockService
    {
        Task<IList<CurrentStock>> GetAll(string? targetTenantId = null);
        Task UpdateStock(int itemId, string batch, decimal qtyChange, DateTime? expiry, decimal mrp, decimal salesRateA, decimal? salesRateB = null,decimal? purchaseRate = null, string? barcode = null, bool forceUpdate = false, string? overrideTenantId = null);
        Task<CurrentStock?> GetStock(int itemId,string batch,DateTime? expiry,decimal mrp);
        Task OverwriteStock(
            int stockId,
            string batch,
            DateTime? expiry,
            decimal mrp,
            decimal qty,
            decimal purchaseRate,
            decimal? salesRateA = null,
            decimal? salesRateB = null,
            string? barcode = null); // MERGED FROM TL
    }
}
