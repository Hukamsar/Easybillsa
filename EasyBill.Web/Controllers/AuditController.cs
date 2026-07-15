using AOne.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyBill.UI.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AuditController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var tenantId = User.FindFirst("TenantId")?.Value;

            var auditRepo = _unitOfWork.GetRepository<AuditLog>();
            IQueryable<AuditLog> query = auditRepo.Query().Include(x => x.Tenant);

            if (!isSuperAdmin)
            {
                query = query.Where(x => x.TenantId == tenantId);
            }

            var logs = await query
                .OrderByDescending(x => x.Timestamp)
                .Take(1000)
                .ToListAsync();

            return View(logs);
        }
    }
}
