using Microsoft.AspNetCore.Mvc;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using AOne.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyBill.Web.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class HoSalesApiController : ControllerBase
    {
        private readonly ITenantRepository _tenantRepo;
        private readonly ApplicationDbContext _dbContext;

        public HoSalesApiController(
            ITenantRepository tenantRepo, 
            ApplicationDbContext dbContext)
        {
            _tenantRepo = tenantRepo;
            _dbContext = dbContext;
        }

        [HttpGet("GetBranches")]
        public async Task<IActionResult> GetBranches()
        {
            var hoTenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(hoTenantId))
                return Unauthorized(new { success = false, message = "HO Tenant ID missing in token." });

            var branches = await _dbContext.Tenants
                .Where(t => t.ParentTenantId == hoTenantId)
                .Select(t => new { t.Id, t.Name, t.BranchCode })
                .ToListAsync();

            return Ok(new { success = true, data = branches });
        }

        [HttpGet("GetSalesLog")]
        public async Task<IActionResult> GetSalesLog(DateTime? fromDate, DateTime? toDate, string? branchId)
        {
            var hoTenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(hoTenantId))
                return Unauthorized(new { success = false, message = "HO Tenant ID missing in token." });

            var branchTenantIds = await _dbContext.Tenants
                .Where(t => t.ParentTenantId == hoTenantId)
                .Select(t => t.Id)
                .ToListAsync();

            var query = _dbContext.Saless
                .IgnoreQueryFilters()
                .Include(s => s.Customers)
                .AsNoTracking()
                .Where(s => branchTenantIds.Contains(s.TenantId) || s.TenantId == hoTenantId);

            if (!string.IsNullOrEmpty(branchId))
            {
                if (branchTenantIds.Contains(branchId) || branchId == hoTenantId)
                {
                    query = query.Where(s => s.TenantId == branchId);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid branch access." });
                }
            }

            if (fromDate.HasValue)
                query = query.Where(s => s.BillDate != null && s.BillDate.Value.Date >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(s => s.BillDate != null && s.BillDate.Value.Date <= toDate.Value.Date);

            var salesList = await query
                .OrderByDescending(s => s.BillDate)
                .Select(s => new
                {
                    s.Id,
                    s.BillNo,
                    s.BillDate,
                    TotalQty = s.SalesItems.Sum(si => si.Qty),
                    s.Total,
                    TotalDiscount = s.Totaldiscount,
                    GstAmt = s.TotalGstAmt,
                    TotalRoundofAmount = s.RoundOffAmount,
                    NetTotal = s.TotalPayable,
                    s.TenantId,
                    CustomerName = s.Customers != null ? s.Customers.Name : "",
                    MobileNumber = s.MobileNo,
                    s.PaymentStatus,
                    s.PaymentType
                })
                .ToListAsync();

            var tenantNames = await _dbContext.Tenants
                .Where(t => branchTenantIds.Contains(t.Id) || t.Id == hoTenantId)
                .ToDictionaryAsync(t => t.Id, t => t.Name);

            var result = salesList.Select(s => new
            {
                s.Id,
                s.BillNo,
                s.BillDate,
                s.TotalQty,
                s.Total,
                s.TotalDiscount,
                s.GstAmt,
                s.TotalRoundofAmount,
                s.NetTotal,
                s.TenantId,
                s.CustomerName,
                s.MobileNumber,
                s.PaymentStatus,
                s.PaymentType,
                BranchName = !string.IsNullOrEmpty(s.TenantId) && tenantNames.ContainsKey(s.TenantId) 
                             ? tenantNames[s.TenantId] 
                             : "Unknown"
            }).ToList();

            return Ok(new { success = true, data = result });
        }

        [HttpGet("GetInvoiceDetails/{id}")]
        public async Task<IActionResult> GetInvoiceDetails(int id)
        {
            var hoTenantId = User.FindFirst("TenantId")?.Value;
            if (string.IsNullOrEmpty(hoTenantId))
                return Unauthorized(new { success = false, message = "HO Tenant ID missing in token." });

            var branchTenantIds = await _dbContext.Tenants
                .Where(t => t.ParentTenantId == hoTenantId)
                .Select(t => t.Id)
                .ToListAsync();

            var sale = await _dbContext.Saless
                .IgnoreQueryFilters()
                .Include(s => s.SalesItems)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null)
                return NotFound(new { success = false, message = "Sale not found." });

            if (sale.TenantId != hoTenantId && (!string.IsNullOrEmpty(sale.TenantId) && !branchTenantIds.Contains(sale.TenantId)))
                return Unauthorized(new { success = false, message = "You do not have permission to view this invoice." });

            var itemMasterIds = sale.SalesItems?.Select(si => si.ItemMasterId).Distinct().ToList() ?? new List<int>();
            var items = await _dbContext.ItemMasters
                .Where(im => itemMasterIds.Contains(im.Id))
                .ToDictionaryAsync(im => im.Id, im => im.Name);

            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == sale.TenantId);

            var result = new
            {
                sale.Id,
                sale.BillNo,
                sale.BillDate,
                CustomerName = sale.Customers?.Name ?? "",
                MobileNumber = sale.MobileNo,
                NetTotal = sale.TotalPayable,
                TotalDiscount = sale.Totaldiscount,
                TotalRoundofAmount = sale.RoundOffAmount,
                GstAmt = sale.TotalGstAmt,
                sale.PaymentType,
                BranchName = tenant?.Name ?? "Unknown",
                BranchAddress = tenant?.Address1,
                BranchPhone = tenant?.Phone,
                BranchEmail = tenant?.Email,
                Items = sale.SalesItems?.Select(si => new
                {
                    si.Id,
                    si.ItemMasterId,
                    ItemName = items.ContainsKey(si.ItemMasterId) ? items[si.ItemMasterId] : "Unknown Item",
                    si.Qty,
                    FreeQty = 0,
                    si.Rate,
                    si.Discount,
                    GstAmt = si.Gst,
                    CessAmt = si.Cess,
                    si.Amount
                })
            };

            return Ok(new { success = true, data = result });
        }
    }
}
