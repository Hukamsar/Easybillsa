using AOne.DataAccess.Data;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AOneWeb.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class CompanyPlanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CompanyPlanController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var plans = await _context.SubscriptionPlans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .ToListAsync();
            return View(plans);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
            return View(new SubscriptionPlan());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubscriptionPlan plan, List<int> selectedFeatures)
        {
            if (ModelState.IsValid)
            {
                _context.SubscriptionPlans.Add(plan);
                await _context.SaveChangesAsync();

                if (selectedFeatures != null && selectedFeatures.Any())
                {
                    foreach (var featureId in selectedFeatures)
                    {
                        _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featureId });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["success"] = "Subscription plan created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
            return View(plan);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null)
            {
                return NotFound();
            }

            var selectedFeatureIds = await _context.PlanFeatures
                .Where(pf => pf.PlanId == id)
                .Select(pf => pf.FeatureId)
                .ToListAsync();

            ViewBag.SelectedFeatureIds = selectedFeatureIds;
            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();

            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubscriptionPlan plan, List<int> selectedFeatures)
        {
            if (ModelState.IsValid)
            {
                _context.SubscriptionPlans.Update(plan);

                var existingFeatures = _context.PlanFeatures.Where(pf => pf.PlanId == plan.Id);
                _context.PlanFeatures.RemoveRange(existingFeatures);

                if (selectedFeatures != null && selectedFeatures.Any())
                {
                    foreach (var featureId in selectedFeatures)
                    {
                        _context.PlanFeatures.Add(new PlanFeature { PlanId = plan.Id, FeatureId = featureId });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["success"] = "Subscription plan updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SelectedFeatureIds = selectedFeatures ?? new List<int>();
            ViewBag.Features = await _context.Features.OrderBy(f => f.FeatureKey).ToListAsync();
            return View(plan);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return Json(new { success = false, message = "Plan not found." });
                }

                var planFeatures = _context.PlanFeatures.Where(pf => pf.PlanId == id);
                _context.PlanFeatures.RemoveRange(planFeatures);

                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Plan deleted successfully." });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetPlanFeatures(int planId)
        {
            var features = await _context.PlanFeatures
                .Where(pf => pf.PlanId == planId && pf.Feature != null)
                .Select(pf => pf.Feature!.FeatureKey)
                .ToListAsync();
            return Json(features);
        }
    }
}
