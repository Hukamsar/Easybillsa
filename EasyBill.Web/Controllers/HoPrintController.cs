using Microsoft.AspNetCore.Mvc;
using EasyBill.Models.Entity;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace EasyBill.Web.Controllers
{
    // We use JWT authentication for HO because this is accessed via the Angular app's tokens
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class HoPrintController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public HoPrintController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(int? id)
        {
            if (id == null) return BadRequest("Invoice ID is missing.");

            var hoTenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(hoTenantId))
                return Unauthorized("HO Tenant ID missing in token.");

            var branchTenantIds = await _dbContext.Tenants
                .Where(t => t.ParentTenantId == hoTenantId)
                .Select(t => t.Id)
                .ToListAsync();

            // Fetch the sale using EF Core (bypassing the SP that blocks cross-tenant access)
            var saleData = await _dbContext.Saless
                .IgnoreQueryFilters()
                .Include(s => s.SalesItems)
                .ThenInclude(si => si.ItemMaster)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (saleData == null)
                return NotFound("Invoice not found.");

            if (saleData.TenantId != hoTenantId && (!string.IsNullOrEmpty(saleData.TenantId) && !branchTenantIds.Contains(saleData.TenantId)))
                return Unauthorized("You do not have permission to view this invoice.");

            // EasyBill requires these ViewBags for the print template
            ViewBag.CustomerPointsBalance = 0;
            ViewBag.PointsEarned = 0;
            ViewBag.PointsRedeemed = 0;
            ViewBag.HidePrintControls = true;

            // Fetch tenant setting to know which paper size to use (Thermal, A4, A5)
            var salesSetting = await _dbContext.SalseSettings
                .FirstOrDefaultAsync(x => x.TenantId == saleData.TenantId);
                
            string paperSize = salesSetting?.PrintType ?? "A4";

            if (paperSize == "Thermal")
                return View("~/Views/Shared/InvoiceTemplates/Thermal_Print.cshtml", saleData);
            if (paperSize == "A5")
                return View("~/Views/Shared/InvoiceTemplates/A5_Print.cshtml", saleData);
            
            return View("~/Views/Shared/InvoiceTemplates/A4_Print.cshtml", saleData);
        }
    }
}
