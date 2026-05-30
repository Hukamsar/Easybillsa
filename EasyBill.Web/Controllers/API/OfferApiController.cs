using AOne.DataAccess.ProfileService;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class OfferApiController : ControllerBase
    {
        private readonly IOfferRepository _offerrepo;
        private readonly IItemMasterRepository _itemmasterrepo;
        private readonly ICompanyRepository _companyrepo;
        private readonly ICategoryMasterRepository _categorymasterrepo;
        private readonly IProfileService _profileservice;

        public OfferApiController(
            IOfferRepository offerrepo,
            IItemMasterRepository itemmasterrepo,
            ICompanyRepository companyrepo,
            ICategoryMasterRepository categorymasterrepo,
            IProfileService profileService)
        {
            _offerrepo = offerrepo;
            _itemmasterrepo = itemmasterrepo;
            _companyrepo = companyrepo;
            _categorymasterrepo = categorymasterrepo;
            _profileservice = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await _profileservice.Set(User);
            var data = await _offerrepo.GetAll();
           
            var offerdata = data.Select(x => new OfferVM
            {
                Id = x.Id,
                OfferName = x.OfferName,
                OfferType = x.OfferType,
                Applicable = x.Applicable,
                DiscountValue = x.DiscountValue,
                MinAmount = x.MinAmount,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                IsActive = x.IsActive,
                CompanyId = x.CompanyId,
                CategoryId = x.CategoryId,
                ItemId = x.ItemId,
                OfferItems = x.OfferItems.Select(oi => new OfferItemVM
                {
                    Id = oi.Id,
                    OfferId = oi.OfferId,
                    ItemId = oi.ItemId,
                    FreeItemId = oi.FreeItemId,
                    BuyQty = oi.BuyQty,
                    FreeQty = oi.FreeQty,
                    fixedPrice = oi.fixedPrice,
                    ComboGroupId = oi.ComboGroupId
                }).ToList()
            }).ToList();

            return Ok(new { success = true, data = offerdata });
        }

 
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] OfferVM VM)
        {
            if (VM == null)
                return BadRequest("Offer data is null.");

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
            return Ok(new { success = true, message = "Offer created successfully." });
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> GetEditData(int id)
        {
            var model = await _offerrepo.GetById(id);
            if (model == null)
                return NotFound("Offer not found.");

            var VM = new OfferVM
            {
                Id = model.Id,
                OfferName = model.OfferName,
                OfferType = model.OfferType,
                Applicable = model.Applicable,
                DiscountValue = model.DiscountValue,
                MinAmount = model.MinAmount,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                CompanyId = model.CompanyId,
                CategoryId = model.CategoryId,
                ItemId = model.ItemId,
                IsActive = model.IsActive,
                OfferItems = model.OfferItems.Select(x => new OfferItemVM
                {
                    Id = x.Id,
                    ItemId = x.ItemId,
                    FreeItemId = x.FreeItemId,
                    fixedPrice = x.fixedPrice,
                    BuyQty = x.BuyQty,
                    FreeQty = x.FreeQty,
                    ComboGroupId = x.ComboGroupId,
                }).ToList() ?? new List<OfferItemVM>()
            };
             
            return Ok(new
            {
                Offer = VM,
              
            });
        }

        [HttpPost("Edit")]
        public async Task<IActionResult> Edit([FromBody] OfferVM VM)
        {
            var model = await _offerrepo.GetById(VM.Id);
            if (model == null)
                return NotFound("Offer not found.");

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

            // Remove deleted items
            var removedItems = model.OfferItems.Where(dbItem => !VM.OfferItems.Any(vmItem => vmItem.Id == dbItem.Id)).ToList();
            foreach (var item in removedItems)
                model.OfferItems.Remove(item);

            // Add or update items
            foreach (var item in VM.OfferItems)
            {
                if (item.Id > 0)
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
                else
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
            return Ok(new { success = true, message = "Offer updated successfully." });
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Invalid Id for deletion." });

            var model = await _offerrepo.GetById(id);
            if (model == null)
                return NotFound(new { success = false, message = "Offer not found." });

            await _offerrepo.Delete(model);
            return Ok(new { success = true, message = "Offer deleted successfully." });
        }
    }
}
