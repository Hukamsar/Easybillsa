using Microsoft.AspNetCore.Mvc;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EasyBill.UI.Filters;

namespace EasyBill.Web.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderApiController : ControllerBase
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly ITenantRepository _tenantRepository;

        public PurchaseOrderApiController(
            IPurchaseOrderRepository poRepository,
            ITenantRepository tenantRepository)
        {
            _poRepository = poRepository;
            _tenantRepository = tenantRepository;
        }

        // GET: api/PurchaseOrderApi/pending-branch-pos?hoTenantId=XYZ
        [HttpGet("pending-branch-pos")]
        [HeadOfficeOnly]
        public async Task<IActionResult> GetPendingBranchPOs(string hoTenantId)
        {
            if (string.IsNullOrEmpty(hoTenantId))
                return BadRequest(new { success = false, message = "HO TenantId is required." });

            var allPOs = await _poRepository.GetAll();
            var pendingPOs = allPOs.Where(p => p.TargetTenantId == hoTenantId && p.WorkflowStatus == "Pending_HO").ToList();

            return Ok(new { success = true, data = pendingPOs });
        }

        public class ConsolidateRequest
        {
            public string HoTenantId { get; set; }
            public List<int> BranchPoIds { get; set; }
            public string DeliveryType { get; set; } // "CentralWarehouse" or "DirectToBranch"
        }

        // POST: api/PurchaseOrderApi/consolidate
        [HttpPost("consolidate")]
        [HeadOfficeOnly]
        public async Task<IActionResult> ConsolidatePOs([FromBody] ConsolidateRequest request)
        {
            if (request == null || !request.BranchPoIds.Any())
                return BadRequest(new { success = false, message = "Invalid consolidation request." });

            var allPOs = await _poRepository.GetAll();
            var branchPOs = allPOs.Where(p => request.BranchPoIds.Contains(p.Id) && p.WorkflowStatus == "Pending_HO").ToList();

            if (!branchPOs.Any())
                return NotFound(new { success = false, message = "No valid pending Branch POs found to consolidate." });

            // Create Master PO
            var masterPO = new PurchaseOrder
            {
                TenantId = request.HoTenantId,
                WorkflowStatus = "Merged_By_HO",
                DeliveryType = request.DeliveryType,
                BillDate = System.DateTime.Now,
                PurchaseType = "Consolidated_DropShip",
                TotalPayable = branchPOs.Sum(p => p.TotalPayable),
                Balance = branchPOs.Sum(p => p.TotalPayable)
            };

            await _poRepository.Create(masterPO);

            // Update Branch POs to point to Master
            foreach(var po in branchPOs)
            {
                po.WorkflowStatus = "Merged_By_HO";
                po.ParentPurchaseOrderId = masterPO.Id;
                await _poRepository.Update(po);
            }

            return Ok(new { success = true, message = "POs consolidated successfully.", masterPoId = masterPO.Id });
        }

        // GET: api/PurchaseOrderApi/master-pos?hoTenantId=XYZ
        [HttpGet("master-pos")]
        [HeadOfficeOnly]
        public async Task<IActionResult> GetMasterPOs(string hoTenantId)
        {
            if (string.IsNullOrEmpty(hoTenantId))
                return BadRequest(new { success = false, message = "HO TenantId is required." });

            var allPOs = await _poRepository.GetAll();
            var masterPOs = allPOs.Where(p => p.TenantId == hoTenantId && p.PurchaseType == "Consolidated_DropShip").ToList();

            return Ok(new { success = true, data = masterPOs });
        }
    }
}
