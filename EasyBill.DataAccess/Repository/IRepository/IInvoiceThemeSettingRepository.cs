using EasyBill.Models.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository.IRepository
{
    public interface IInvoiceThemeSettingRepository
    {
        Task<IList<InvoiceThemeSetting>> GetAllThemesByUserIdAsync(string userId);

        Task<InvoiceThemeSetting> GetThemeSettingAsync(string userId, string paperSize);

        Task<InvoiceThemeSetting> GetDefaultThemeAsync(string userId);

        Task<InvoiceThemeSetting> SaveOrUpdateThemeAsync(InvoiceThemeSetting model);

        Task SetDefaultThemeAsync(string userId, string paperSize);
    }
}