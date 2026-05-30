using AOne.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class InvoiceThemeSettingRepository : IInvoiceThemeSettingRepository
    {
        private readonly IUnitOfWork _unitofwork;

        public InvoiceThemeSettingRepository(IUnitOfWork unitofwork)
        {
            _unitofwork = unitofwork;
        }

        public async Task<IList<InvoiceThemeSetting>> GetAllThemesByUserIdAsync(string userId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceThemeSetting>();
               IList<InvoiceThemeSetting> result =  await repository.Query()
                    .Where(l => l.ApplicationUserId == userId)
                    .ToListAsync();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<InvoiceThemeSetting> GetThemeSettingAsync(string userId, string paperSize)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceThemeSetting>();

                return await repository.Query()
                    .AsNoTracking()
                    .Where(l => l.ApplicationUserId == userId)
                    .OrderByDescending(l => l.UpdatedAt ?? DateTime.MinValue)
                    .ThenByDescending(l => l.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<InvoiceThemeSetting> GetDefaultThemeAsync(string userId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceThemeSetting>();
                return await repository.Query()
                    .AsNoTracking()
                    .Where(l => l.ApplicationUserId == userId)
                    .OrderByDescending(l => l.UpdatedAt ?? DateTime.MinValue)
                    .ThenByDescending(l => l.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<InvoiceThemeSetting> SaveOrUpdateThemeAsync(InvoiceThemeSetting model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<InvoiceThemeSetting>();

                if (model.Id == 0)
                {
                    repository.Add(model);
                }
                else
                {
                    repository.Update(model);
                }

                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }

                return model;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SetDefaultThemeAsync(string userId, string paperSize)
        {
            try
            {
                var normalized = NormalizePaperSize(paperSize);
                var repository = _unitofwork.GetRepository<InvoiceThemeSetting>();
                var allThemes = await repository.Query()
                    .Where(l => l.ApplicationUserId == userId)
                    .OrderByDescending(l => l.UpdatedAt ?? DateTime.MinValue)
                    .ThenByDescending(l => l.Id)
                    .ToListAsync();

                if (allThemes.Count == 0)
                {
                    return;
                }

                foreach (var theme in allThemes)
                {
                    theme.IsDefault = false;
                    repository.Update(theme);
                }

                var activeTheme = allThemes[0];
                activeTheme.IsDefault = true;
                activeTheme.PaperSize = normalized switch
                {
                    "a5" => "A5",
                    "thermal" => "Thermal",
                    _ => "A4"
                };
                repository.Update(activeTheme);

                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string NormalizePaperSize(string paperSize)
        {
            return string.IsNullOrWhiteSpace(paperSize) ? "a4" : paperSize.Trim().ToLower();
        }
    }
}
