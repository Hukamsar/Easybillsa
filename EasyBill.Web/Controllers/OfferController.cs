using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class OfferController : Controller
    {
        private readonly IOfferRepository _offerrepo;
        private readonly IItemMasterRepository _itemmasterrepo;
        private readonly ICompanyRepository _companyrepo;
        private readonly ICategoryMasterRepository _categorymasterrepo;
        public OfferController(
            IOfferRepository offerrepo, 
            IItemMasterRepository itemmasterrepo,
            ICompanyRepository companyrepo,
            ICategoryMasterRepository categorymasterrepo)
        {
            _offerrepo = offerrepo;
            _itemmasterrepo = itemmasterrepo;
            _companyrepo = companyrepo;
            _categorymasterrepo = categorymasterrepo;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _offerrepo.GetAll(); 
            return View(data);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new OfferVM();
            var itemdata = await _itemmasterrepo.GetAll();
            var companydata = await _companyrepo.GetAll();
            var categorydata = await _categorymasterrepo.GetAll();
            ViewBag.Item = new SelectList(itemdata, "Id", "Name");
            ViewBag.Company = new SelectList(companydata, "Id", "Name");
            ViewBag.Category = new SelectList(categorydata, "Id", "CategoryName");
            ViewBag.OfferType = Enum.GetValues(typeof(OfferType)) .Cast<OfferType>()
                             .Select(s => new SelectListItem
                             {
                                 Text = s.ToString(),
                                 Value = s.ToString()
                             });
            ViewBag.Applicable = Enum.GetValues(typeof(Applicable)).Cast<Applicable>()
                            .Select(s => new SelectListItem
                            {
                                Text = s.ToString(),
                                Value = s.ToString()
                            });
            viewModel.StartDate = DateTime.Today;
            viewModel.EndDate = DateTime.Now.AddMonths(1);
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(OfferVM VM)
        {
            if (VM != null)
            {
                var model = new Offer
                {
                    OfferName = VM.OfferName,
                    OfferType = VM.OfferType,
                    Applicable = VM.Applicable,
                    DiscountValue = VM.DiscountValue,
                    MinAmount = VM.MinAmount,
                    StartDate = VM.StartDate,
                    EndDate = VM.EndDate,
                    StartTime = VM.StartTime,
                    EndTime = VM.EndTime,
                    CompanyId = VM.CompanyId,
                    CategoryId = VM.CategoryId,
                    ItemId = VM.ItemId,
                    IsActive = VM.IsActive,
                    OfferItems = VM.OfferItems?.Select(x => new OfferItem
                    {
                        ItemId = x.ItemId,
                        FreeItemId = x.FreeItemId,
                        fixedPrice = x.fixedPrice,
                        BuyQty = x.BuyQty,
                        FreeQty = x.FreeQty,
                        ComboGroupId = x.ComboGroupId,
                    }).ToList() ?? new List<OfferItem>()
                };
                await _offerrepo.Create(model);
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int Id)
        {
            Offer model = await _offerrepo.GetById(Id);
            OfferVM VM = new OfferVM();
            if (model != null)
            {
                VM.Id = model.Id;
                VM.OfferName = model.OfferName;
                VM.OfferType = model.OfferType;
                VM.Applicable = model.Applicable;
                VM.DiscountValue = model.DiscountValue;
                VM.MinAmount = model.MinAmount;
                VM.StartDate = model.StartDate;
                VM.EndDate = model.EndDate;
                VM.StartTime = model.StartTime;
                VM.EndTime = model.EndTime;
                VM.CompanyId = model.CompanyId;
                VM.CategoryId = model.CategoryId;
                VM.ItemId = model.ItemId;
                VM.IsActive = model.IsActive;
                VM.OfferItems = model.OfferItems.Select(x => new OfferItemVM
                {
                    Id = x.Id,
                    ItemId = x.ItemId,
                    FreeItemId = x.FreeItemId,
                    fixedPrice = x.fixedPrice,
                    BuyQty = x.BuyQty,
                    FreeQty = x.FreeQty,
                    ComboGroupId = x.ComboGroupId,
                }).ToList() ?? new List<OfferItemVM>();
            }
            var itemdata = await _itemmasterrepo.GetAll();
            var companydata = await _companyrepo.GetAll();
            var categorydata = await _categorymasterrepo.GetAll();
            ViewBag.Item = new SelectList(itemdata, "Id", "Name");
            ViewBag.Company = new SelectList(companydata, "Id", "Name");
            ViewBag.Category = new SelectList(categorydata, "Id", "CategoryName");
            ViewBag.OfferType = Enum.GetValues(typeof(OfferType)).Cast<OfferType>()
                             .Select(s => new SelectListItem
                             {
                                 Text = s.ToString(),
                                 Value = s.ToString()
                             });
            ViewBag.Applicable = Enum.GetValues(typeof(Applicable)).Cast<Applicable>()
                            .Select(s => new SelectListItem
                            {
                                Text = s.ToString(),
                                Value = s.ToString()
                            });
            return View(VM);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(OfferVM VM)
        {
            Offer model = await _offerrepo.GetById(VM.Id);
            if (model != null)
            {
                model.Id = VM.Id;
                model.OfferName = VM.OfferName;
                model.OfferType = VM.OfferType;
                model.Applicable = VM.Applicable;
                model.DiscountValue = VM.DiscountValue;
                model.MinAmount = VM.MinAmount;
                model.StartDate = VM.StartDate;
                model.EndDate = VM.EndDate;
                model.StartTime = VM.StartTime;
                model.EndTime = VM.EndTime;
                model.CompanyId = VM.CompanyId;
                model.CategoryId = VM.CategoryId;
                model.ItemId = VM.ItemId;
                model.IsActive = VM.IsActive;
                var removedItems = model.OfferItems.Where(dbItem => !VM.OfferItems.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
                foreach (var item in removedItems)
                {
                    model.OfferItems.Remove(item);
                }
                foreach (var item in VM.OfferItems)
                {
                    if (item.Id > 0) // Update existing items
                    {
                        var existingItem = model.OfferItems.FirstOrDefault(x => x.Id == item.Id);
                        if (existingItem != null)
                        {
                            existingItem.ItemId = item.ItemId;
                            existingItem.FreeItemId = item.FreeItemId;
                            existingItem.fixedPrice = item.fixedPrice;
                            existingItem.BuyQty = item.BuyQty;
                            existingItem.FreeQty = item.FreeQty; 
                            existingItem.ComboGroupId = item.ComboGroupId;
                        }
                    }
                    else // Add new items
                    {
                        model.OfferItems.Add(new OfferItem
                        {
                            ItemId = item.ItemId,
                            FreeItemId = item.FreeItemId,
                            fixedPrice = item.fixedPrice,
                            BuyQty = item.BuyQty, 
                            FreeQty = item.FreeQty,
                            ComboGroupId = item.ComboGroupId,
                        });
                    }
                }
                await _offerrepo.Update(model);
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid Id for deletion." });
                }
                var model = await _offerrepo.GetById(id);

                if (model == null)
                {
                    return Json(new { success = false, message = "Item not found." });
                }
                await _offerrepo.Delete(model);

                return Json(new { success = true, message = "Item deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        public async Task<JsonResult> CheckDuplicate(string OfferName, int id)
        {
            bool result = await _offerrepo.CheckDuplicateAsync(OfferName, id);

            return Json(new
            {
                exists = result
            });
        }
    }
}
