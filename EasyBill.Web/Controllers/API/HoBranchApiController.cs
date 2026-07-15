using Microsoft.AspNetCore.Mvc;
using AOne.DataAccess.Data;
using AOne.Models.Entity;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.Web.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class HoBranchApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HoBranchApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/HoBranchApi/branches?hoTenantId=XYZ
        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches(string hoTenantId)
        {
            if (string.IsNullOrEmpty(hoTenantId))
                return BadRequest(new { success = false, message = "HO TenantId is required." });

            var hoTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == hoTenantId);
            if (hoTenant == null)
                return NotFound(new { success = false, message = "Head Office tenant not found." });

            var branches = await _context.Tenants
                .Where(t => t.ParentTenantId == hoTenantId && t.CompanyType == AOne.Utility.Enums.CompanyType.Branch)
                .ToListAsync();

            var branchList = branches.Select(b => new {
                b.Id,
                b.Name,
                b.Email,
                b.MobileNo,
                Address = b.FullAddress
            }).ToList();

            return Ok(new {
                success = true,
                branches = branchList
            });
        }

        // 4. GET: api/HoBranchApi/assigned-items?branchTenantId=XYZ
        [HttpGet("assigned-items")]
        public async Task<IActionResult> GetAssignedItems(string branchTenantId)
        {
            if (string.IsNullOrEmpty(branchTenantId))
                return BadRequest(new { success = false, message = "Branch TenantId is required." });

            var assignedItemIds = await _context.BranchItemMappings
                .Where(m => m.TenantId == branchTenantId && m.IsActive)
                .Select(m => m.ItemMasterId)
                .ToListAsync();

            return Ok(new { success = true, data = assignedItemIds });
        }

        // 5. POST: api/HoBranchApi/assign-items
        [HttpPost("assign-items")]
        public async Task<IActionResult> AssignItemsToBranch([FromBody] AssignItemsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.BranchTenantId) || request.ItemMasterIds == null)
                return BadRequest(new { success = false, message = "Invalid request payload." });

            // Fetch ALL existing mappings including soft-deleted ones
            var existingMappings = await _context.BranchItemMappings
                .IgnoreQueryFilters()
                .Where(m => m.TenantId == request.BranchTenantId)
                .ToListAsync();

            // Mark all existing as inactive and deleted (to remove them from branch view)
            foreach (var mapping in existingMappings)
            {
                mapping.IsActive = false;
                mapping.Deleted = DateTime.Now;
            }

            // Un-delete and activate requested items, or add them if they don't exist
            foreach (var id in request.ItemMasterIds)
            {
                var existing = existingMappings.FirstOrDefault(m => m.ItemMasterId == id);
                if (existing != null)
                {
                    existing.IsActive = true;
                    existing.Deleted = null; // Un-delete it
                }
                else
                {
                    _context.BranchItemMappings.Add(new BranchItemMapping
                    {
                        TenantId = request.BranchTenantId,
                        ItemMasterId = id,
                        IsActive = true,
                        Deleted = null
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Items assigned to branch successfully." });
        }
    }

    public class AssignItemsRequest
    {
        public string BranchTenantId { get; set; }
        public List<int> ItemMasterIds { get; set; }
    }
}
