using Microsoft.AspNetCore.Mvc;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using AOne.DataAccess.Data;
using AOne.Utility.Enums;
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
    public class HoOfferApiController : ControllerBase
    {
        private readonly IOfferRepository _offerRepo;
        private readonly ITenantRepository _tenantRepo;
        private readonly ApplicationDbContext _dbContext;

        public HoOfferApiController(
            IOfferRepository offerRepo, 
            ITenantRepository tenantRepo, 
            ApplicationDbContext dbContext)
        {
            _offerRepo = offerRepo;
            _tenantRepo = tenantRepo;
            _dbContext = dbContext;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var currentTenantId = User.FindFirst("TenantId")?.Value;
            var query = _dbContext.Offers.Include(o => o.OfferItems).AsQueryable();

            if (!string.IsNullOrEmpty(currentTenantId))
            {
                query = query.Where(o => o.TenantId == currentTenantId);
            }

            var offers = await query.ToListAsync();

            var mappings = await _dbContext.OfferStoreMappings.ToListAsync();
            var tenants = await _tenantRepo.GetAll();

            var result = offers.Select(o => new
            {
                o.Id,
                o.OfferName,
                OfferType = o.OfferType?.ToString() ?? "DiscountPercent",
                Applicable = o.Applicable?.ToString() ?? "AllProduct",
                o.DiscountValue,
                o.MinAmount,
                o.StartDate,
                o.EndDate,
                StartTime = o.StartTime?.ToString() ?? "00:00:00",
                EndTime = o.EndTime?.ToString() ?? "23:59:59",
                o.IsActive,
                o.CompanyId,
                o.CategoryId,
                o.ItemId,
                OfferItems = o.OfferItems.Select(oi => new
                {
                    oi.Id,
                    oi.ItemId,
                    oi.FreeItemId,
                    oi.BuyQty,
                    oi.FreeQty,
                    oi.fixedPrice,
                    ComboGroupId = int.TryParse(oi.ComboGroupId, out var cId) ? cId : 0
                }).ToList(),
                SelectedTenants = mappings
                    .Where(m => m.OfferId == o.Id)
                    .Select(m => m.TenantId)
                    .ToList(),
                SelectedTenantNames = tenants
                    .Where(t => mappings.Any(m => m.OfferId == o.Id && m.TenantId == t.Id))
                    .Select(t => t.Name)
                    .ToList()
            });

            return Ok(new { success = true, data = result });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] HoOfferCreateRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OfferName))
                return BadRequest(new { success = false, message = "Invalid offer parameters." });

            var currentTenantId = User.FindFirst("TenantId")?.Value;

            var offer = new Offer
            {
                OfferName = request.OfferName,
                DiscountValue = request.DiscountValue,
                MinAmount = request.MinAmount,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = request.IsActive,
                CompanyId = request.CompanyId,
                CategoryId = request.CategoryId,
                ItemId = request.ItemId,
                TenantId = currentTenantId
            };

            // Parse enums
            if (Enum.TryParse<OfferType>(request.OfferType, true, out var oType))
                offer.OfferType = oType;
            else
                offer.OfferType = OfferType.DiscountPercent;

            if (Enum.TryParse<Applicable>(request.Applicable, true, out var app))
                offer.Applicable = app;
            else
                offer.Applicable = Applicable.AllProduct;

            offer.BuyQty = request.BuyQty;
            offer.FreeQty = request.FreeQty;

            // Parse TimeSpan
            if (TimeSpan.TryParse(request.StartTime, out var sTime))
                offer.StartTime = sTime;
            if (TimeSpan.TryParse(request.EndTime, out var eTime))
                offer.EndTime = eTime;

            // Add Items
            offer.OfferItems = request.OfferItems?.Select(oi => new OfferItem
            {
                ItemId = oi.ItemId,
                FreeItemId = oi.FreeItemId > 0 ? oi.FreeItemId : null,
                BuyQty = (int)oi.BuyQty,
                FreeQty = (int)oi.FreeQty,
                fixedPrice = oi.fixedPrice,
                ComboGroupId = oi.ComboGroupId
            }).ToList() ?? new List<OfferItem>();

            _dbContext.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();

            // Save Store Mappings
            if (request.TenantIds != null && request.TenantIds.Any())
            {
                foreach (var tenantId in request.TenantIds)
                {
                    _dbContext.OfferStoreMappings.Add(new OfferStoreMapping
                    {
                        OfferId = offer.Id,
                        TenantId = tenantId
                    });
                }
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { success = true, message = "Offer created successfully with store mappings." });
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit([FromBody] HoOfferEditRequest request)
        {
            if (request == null || request.Id <= 0)
                return BadRequest(new { success = false, message = "Invalid offer details." });

            var currentTenantId = User.FindFirst("TenantId")?.Value;
            var query = _dbContext.Offers.Include(o => o.OfferItems).AsQueryable();

            if (!string.IsNullOrEmpty(currentTenantId))
            {
                query = query.Where(o => o.TenantId == currentTenantId);
            }

            var offer = await query.FirstOrDefaultAsync(o => o.Id == request.Id);

            if (offer == null)
                return NotFound(new { success = false, message = "Offer not found or access denied." });

            offer.OfferName = request.OfferName;
            offer.DiscountValue = request.DiscountValue;
            offer.MinAmount = request.MinAmount;
            offer.StartDate = request.StartDate;
            offer.EndDate = request.EndDate;
            offer.IsActive = request.IsActive;
            offer.CompanyId = request.CompanyId;
            offer.CategoryId = request.CategoryId;
            offer.ItemId = request.ItemId;

            // Parse enums
            if (Enum.TryParse<OfferType>(request.OfferType, true, out var oType))
                offer.OfferType = oType;
            if (Enum.TryParse<Applicable>(request.Applicable, true, out var app))
                offer.Applicable = app;

            offer.BuyQty = request.BuyQty;
            offer.FreeQty = request.FreeQty;

            // Parse TimeSpan
            if (TimeSpan.TryParse(request.StartTime, out var sTime))
                offer.StartTime = sTime;
            if (TimeSpan.TryParse(request.EndTime, out var eTime))
                offer.EndTime = eTime;

            // Update items
            var existingItems = await _dbContext.OfferItems.Where(oi => oi.OfferId == offer.Id).ToListAsync();
            _dbContext.OfferItems.RemoveRange(existingItems);

            if (request.OfferItems != null)
            {
                foreach (var oi in request.OfferItems)
                {
                    _dbContext.OfferItems.Add(new OfferItem
                    {
                        OfferId = offer.Id,
                        ItemId = oi.ItemId,
                        FreeItemId = oi.FreeItemId > 0 ? oi.FreeItemId : null,
                        BuyQty = (int)oi.BuyQty,
                        FreeQty = (int)oi.FreeQty,
                        fixedPrice = oi.fixedPrice,
                        ComboGroupId = oi.ComboGroupId
                    });
                }
            }

            // Update store mappings
            var existingMappings = await _dbContext.OfferStoreMappings
                .Where(m => m.OfferId == offer.Id)
                .ToListAsync();
            _dbContext.OfferStoreMappings.RemoveRange(existingMappings);

            if (request.TenantIds != null && request.TenantIds.Any())
            {
                foreach (var tenantId in request.TenantIds)
                {
                    _dbContext.OfferStoreMappings.Add(new OfferStoreMapping
                    {
                        OfferId = offer.Id,
                        TenantId = tenantId
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { success = true, message = "Offer updated successfully with store mappings." });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentTenantId = User.FindFirst("TenantId")?.Value;
            var query = _dbContext.Offers.AsQueryable();

            if (!string.IsNullOrEmpty(currentTenantId))
            {
                query = query.Where(o => o.TenantId == currentTenantId);
            }

            var offer = await query.FirstOrDefaultAsync(o => o.Id == id);
            if (offer == null)
                return NotFound(new { success = false, message = "Offer not found or access denied." });

            var mappings = await _dbContext.OfferStoreMappings
                .Where(m => m.OfferId == id)
                .ToListAsync();

            var items = await _dbContext.OfferItems
                .Where(i => i.OfferId == id)
                .ToListAsync();

            _dbContext.OfferStoreMappings.RemoveRange(mappings);
            _dbContext.OfferItems.RemoveRange(items);
            _dbContext.Offers.Remove(offer);
            await _dbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Offer deleted successfully." });
        }
    }

    public class HoOfferCreateRequest
    {
        public string OfferName { get; set; }
        public string OfferType { get; set; }
        public string Applicable { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsActive { get; set; }
        public int? BuyQty { get; set; }
        public int? FreeQty { get; set; }
        public int? CompanyId { get; set; }
        public int? CategoryId { get; set; }
        public int? ItemId { get; set; }
        public List<HoOfferItemDto> OfferItems { get; set; }
        public List<string> TenantIds { get; set; }
    }

    public class HoOfferEditRequest : HoOfferCreateRequest
    {
        public int Id { get; set; }
    }

    public class HoOfferItemDto
    {
        public int ItemId { get; set; }
        public int? FreeItemId { get; set; }
        public decimal BuyQty { get; set; }
        public decimal FreeQty { get; set; }
        public decimal fixedPrice { get; set; }
        public string? ComboGroupId { get; set; }
    }
}
