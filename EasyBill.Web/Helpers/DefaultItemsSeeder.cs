using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyBill.UI.Helpers
{
    public class DefaultItemsSeeder
    {
        private readonly IUnitOfWork _unitOfWork;

        public DefaultItemsSeeder(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SeedDefaultItemsForTenantAsync(string tenantId, AOne.Utility.Enums.BusinessType businessType)
        {
            List<SeedItemDto>? itemsToSeed = null;
            if (businessType == AOne.Utility.Enums.BusinessType.Pharmacy)
            {
                itemsToSeed = PharmacyItems;
            }
            else if (businessType == AOne.Utility.Enums.BusinessType.GroceryStore)
            {
                itemsToSeed = GroceryItems;
            }

            if (itemsToSeed == null || !itemsToSeed.Any())
                return;

            var categoryRepo = _unitOfWork.GetRepository<CategoryMaster>();
            var subCategoryRepo = _unitOfWork.GetRepository<SubCategory>();
            var hsnRepo = _unitOfWork.GetRepository<Hsn>();
            var companyRepo = _unitOfWork.GetRepository<Company>();
            var divisionRepo = _unitOfWork.GetRepository<Division>();
            var itemRepo = _unitOfWork.GetRepository<ItemMaster>();

            var validItems = new List<ItemMaster>();

            foreach (var vm in itemsToSeed)
            {
                if (string.IsNullOrWhiteSpace(vm.Name) || string.IsNullOrWhiteSpace(vm.Code))
                    continue;

                // Check for duplicate code for this tenant
                var exists = await itemRepo.Query().AnyAsync(x => x.Code == vm.Code && x.TenantId == tenantId);
                if (exists)
                    continue;

                // 1. Resolve or Create Category
                int? categoryId = null;
                if (!string.IsNullOrEmpty(vm.CategoryName))
                {
                    var category = await categoryRepo.Query()
                        .FirstOrDefaultAsync(x => x.CategoryName.ToLower() == vm.CategoryName.ToLower() && x.TenantId == tenantId);

                    if (category == null)
                    {
                        category = new CategoryMaster
                        {
                            CategoryName = vm.CategoryName,
                            TenantId = tenantId
                        };
                        categoryRepo.Add(category);
                        await _unitOfWork.SaveAsync();
                    }
                    categoryId = category.Id;
                }

                // 2. Resolve or Create SubCategory
                int? subCategoryId = null;
                if (!string.IsNullOrEmpty(vm.SubCategoryName) && categoryId != null)
                {
                    var subCategory = await subCategoryRepo.Query()
                        .FirstOrDefaultAsync(x => x.Name.ToLower() == vm.SubCategoryName.ToLower() && x.CategoryId == categoryId && x.TenantId == tenantId);

                    if (subCategory == null)
                    {
                        subCategory = new SubCategory
                        {
                            Name = vm.SubCategoryName,
                            CategoryId = categoryId.Value,
                            TenantId = tenantId
                        };
                        subCategoryRepo.Add(subCategory);
                        await _unitOfWork.SaveAsync();
                    }
                    subCategoryId = subCategory.Id;
                }

                // 3. Resolve or Create HSN
                int? hsnId = null;
                if (!string.IsNullOrEmpty(vm.HsnCode))
                {
                    var hsn = await hsnRepo.Query()
                        .FirstOrDefaultAsync(x => x.HsnCode == vm.HsnCode && x.TenantId == tenantId);

                    if (hsn == null)
                    {
                        hsn = new Hsn
                        {
                            HsnCode = vm.HsnCode,
                            CGST = vm.CGST,
                            SGST = vm.SGST,
                            IGST = vm.IGST,
                            Cess = vm.Cess,
                            TenantId = tenantId
                        };
                        hsnRepo.Add(hsn);
                        await _unitOfWork.SaveAsync();
                    }
                    hsnId = hsn.Id;
                }

                // 4. Resolve or Create Company
                int? companyId = null;
                if (!string.IsNullOrEmpty(vm.CompanyName))
                {
                    var company = await companyRepo.Query()
                        .FirstOrDefaultAsync(x => x.Name.ToLower() == vm.CompanyName.ToLower() && x.TenantId == tenantId);

                    if (company == null)
                    {
                        company = new Company
                        {
                            Name = vm.CompanyName,
                            TenantId = tenantId
                        };
                        companyRepo.Add(company);
                        await _unitOfWork.SaveAsync();
                    }
                    companyId = company.Id;
                }

                // 5. Resolve or Create Division
                int? divisionId = null;
                if (!string.IsNullOrEmpty(vm.DivisionName) && companyId != null)
                {
                    var division = await divisionRepo.Query()
                        .FirstOrDefaultAsync(x => x.Name.ToLower() == vm.DivisionName.ToLower() && x.CompanyId == companyId && x.TenantId == tenantId);

                    if (division == null)
                    {
                        division = new Division
                        {
                            Name = vm.DivisionName,
                            CompanyId = companyId.Value,
                            TenantId = tenantId
                        };
                        divisionRepo.Add(division);
                        await _unitOfWork.SaveAsync();
                    }
                    divisionId = division.Id;
                }

                // 6. Add Item Master
                var newItem = new ItemMaster
                {
                    Name = vm.Name?.Trim(),
                    Code = vm.Code?.Trim(),
                    Barcode = vm.Barcode,
                    Unit1 = vm.Unit,
                    Packing = vm.Packing,
                    CategoryId = categoryId,
                    SubCategoryId = subCategoryId,
                    DivisionId = divisionId,
                    HsnId = hsnId,
                    CompanyId = companyId,
                    Mrp = vm.Mrp,
                    SalesRate1 = vm.SalesRate1,
                    SalesRate2 = vm.SalesRate2,
                    MinimumQty = vm.MinimumQty,
                    MaximumQty = vm.MaximumQty,
                    ShelfLife = vm.ShelfLife,
                    ShelfLifeUnit = string.IsNullOrWhiteSpace(vm.ShelfLifeUnit) ? "Days" : vm.ShelfLifeUnit,
                    Local = AOne.Utility.Enums.TaxStatus.Taxable,
                    Central = AOne.Utility.Enums.TaxStatus.Taxable,
                    IsActive = true,
                    MaximumDiscount = vm.MaximumDiscount,
                    DecemalAllowed = vm.DecimalAllowed,
                    Conversion = vm.Conversion,
                    Salt = vm.Salt,
                    TenantId = tenantId
                };

                validItems.Add(newItem);
            }

            if (validItems.Any())
            {
                itemRepo.AddRange(validItems);
                await _unitOfWork.SaveAsync();
            }
        }

        private class SeedItemDto
        {
            public string Name { get; set; }
            public string Code { get; set; }
            public string Barcode { get; set; }
            public string Unit { get; set; }
            public string Packing { get; set; }
            public string CategoryName { get; set; }
            public string SubCategoryName { get; set; }
            public string DivisionName { get; set; }
            public string ShelfLifeUnit { get; set; }
            public string HsnCode { get; set; }
            public decimal SGST { get; set; }
            public decimal CGST { get; set; }
            public decimal IGST { get; set; }
            public decimal Cess { get; set; }
            public string CompanyName { get; set; }
            public decimal Mrp { get; set; }
            public decimal SalesRate1 { get; set; }
            public decimal SalesRate2 { get; set; }
            public int MinimumQty { get; set; }
            public int MaximumQty { get; set; }
            public int ShelfLife { get; set; }
            public decimal MaximumDiscount { get; set; }
            public bool DecimalAllowed { get; set; }
            public int Conversion { get; set; }
            public string Salt { get; set; }
        }

        private static readonly List<SeedItemDto> PharmacyItems = new()
        {
            new SeedItemDto
            {
                Name = "Paracetamol 650mg", Code = "PHARM001", Barcode = "1234567890123", Unit = "Tab", Packing = "10s",
                CategoryName = "Tablets", SubCategoryName = "Analgesics", DivisionName = "General Division", ShelfLifeUnit = "Days",
                HsnCode = "30049011", SGST = 6.0m, CGST = 6.0m, IGST = 12.0m, Cess = 0.0m, CompanyName = "GSK",
                Mrp = 30.0m, SalesRate1 = 27.0m, SalesRate2 = 26.0m, MinimumQty = 10, MaximumQty = 100, ShelfLife = 730,
                MaximumDiscount = 10.0m, DecimalAllowed = false, Conversion = 10, Salt = "Paracetamol"
            },
            new SeedItemDto
            {
                Name = "Amoxicillin 500mg", Code = "PHARM002", Barcode = "1234567890124", Unit = "Cap", Packing = "10s",
                CategoryName = "Capsules", SubCategoryName = "Antibiotics", DivisionName = "General Division", ShelfLifeUnit = "Days",
                HsnCode = "30041010", SGST = 6.0m, CGST = 6.0m, IGST = 12.0m, Cess = 0.0m, CompanyName = "Cipla",
                Mrp = 120.0m, SalesRate1 = 105.0m, SalesRate2 = 100.0m, MinimumQty = 5, MaximumQty = 50, ShelfLife = 365,
                MaximumDiscount = 5.0m, DecimalAllowed = false, Conversion = 10, Salt = "Amoxicillin"
            },
            new SeedItemDto
            {
                Name = "Cetirizine 10mg", Code = "PHARM003", Barcode = "1234567890125", Unit = "Tab", Packing = "10s",
                CategoryName = "Tablets", SubCategoryName = "Antihistamines", DivisionName = "General Division", ShelfLifeUnit = "Days",
                HsnCode = "30049099", SGST = 6.0m, CGST = 6.0m, IGST = 12.0m, Cess = 0.0m, CompanyName = "Alkem",
                Mrp = 45.0m, SalesRate1 = 40.0m, SalesRate2 = 38.0m, MinimumQty = 10, MaximumQty = 100, ShelfLife = 730,
                MaximumDiscount = 10.0m, DecimalAllowed = false, Conversion = 10, Salt = "Cetirizine"
            },
            new SeedItemDto
            {
                Name = "Ibuprofen 400mg", Code = "PHARM004", Barcode = "1234567890126", Unit = "Tab", Packing = "10s",
                CategoryName = "Tablets", SubCategoryName = "Analgesics", DivisionName = "General Division", ShelfLifeUnit = "Days",
                HsnCode = "30049012", SGST = 6.0m, CGST = 6.0m, IGST = 12.0m, Cess = 0.0m, CompanyName = "Abbott",
                Mrp = 25.0m, SalesRate1 = 22.0m, SalesRate2 = 21.0m, MinimumQty = 10, MaximumQty = 100, ShelfLife = 730,
                MaximumDiscount = 8.0m, DecimalAllowed = false, Conversion = 10, Salt = "Ibuprofen"
            },
            new SeedItemDto
            {
                Name = "Pantoprazole 40mg", Code = "PHARM005", Barcode = "1234567890127", Unit = "Tab", Packing = "10s",
                CategoryName = "Tablets", SubCategoryName = "Antacids", DivisionName = "General Division", ShelfLifeUnit = "Days",
                HsnCode = "30049039", SGST = 6.0m, CGST = 6.0m, IGST = 12.0m, Cess = 0.0m, CompanyName = "Sun Pharma",
                Mrp = 110.0m, SalesRate1 = 98.0m, SalesRate2 = 95.0m, MinimumQty = 5, MaximumQty = 50, ShelfLife = 540,
                MaximumDiscount = 5.0m, DecimalAllowed = false, Conversion = 10, Salt = "Pantoprazole"
            }
        };

        private static readonly List<SeedItemDto> GroceryItems = new()
        {
            new SeedItemDto
            {
                Name = "Basmati Rice 1kg", Code = "GROC001", Barcode = "9876543210123", Unit = "Kg", Packing = "1kg Packet",
                CategoryName = "Grains", SubCategoryName = "Rice", DivisionName = "Food Division", ShelfLifeUnit = "Days",
                HsnCode = "10063010", SGST = 0.0m, CGST = 0.0m, IGST = 0.0m, Cess = 0.0m, CompanyName = "India Gate",
                Mrp = 150.0m, SalesRate1 = 140.0m, SalesRate2 = 138.0m, MinimumQty = 20, MaximumQty = 200, ShelfLife = 365,
                MaximumDiscount = 0.0m, DecimalAllowed = true, Conversion = 1, Salt = ""
            },
            new SeedItemDto
            {
                Name = "Sunflower Oil 1L", Code = "GROC002", Barcode = "9876543210124", Unit = "Litre", Packing = "Bottle",
                CategoryName = "Edible Oils", SubCategoryName = "Sunflower", DivisionName = "Food Division", ShelfLifeUnit = "Days",
                HsnCode = "15121910", SGST = 2.5m, CGST = 2.5m, IGST = 5.0m, Cess = 0.0m, CompanyName = "Fortune",
                Mrp = 180.0m, SalesRate1 = 170.0m, SalesRate2 = 168.0m, MinimumQty = 10, MaximumQty = 100, ShelfLife = 180,
                MaximumDiscount = 5.0m, DecimalAllowed = false, Conversion = 1, Salt = ""
            },
            new SeedItemDto
            {
                Name = "Sugar 1kg", Code = "GROC003", Barcode = "9876543210125", Unit = "Kg", Packing = "1kg Packet",
                CategoryName = "Sweeteners", SubCategoryName = "Sugar", DivisionName = "Food Division", ShelfLifeUnit = "Days",
                HsnCode = "17011190", SGST = 2.5m, CGST = 2.5m, IGST = 5.0m, Cess = 0.0m, CompanyName = "Madhur",
                Mrp = 48.0m, SalesRate1 = 45.0m, SalesRate2 = 44.0m, MinimumQty = 25, MaximumQty = 250, ShelfLife = 730,
                MaximumDiscount = 0.0m, DecimalAllowed = true, Conversion = 1, Salt = ""
            },
            new SeedItemDto
            {
                Name = "Tata Salt 1kg", Code = "GROC004", Barcode = "9876543210126", Unit = "Kg", Packing = "1kg Packet",
                CategoryName = "Spices", SubCategoryName = "Salt", DivisionName = "Food Division", ShelfLifeUnit = "Days",
                HsnCode = "25010021", SGST = 0.0m, CGST = 0.0m, IGST = 0.0m, Cess = 0.0m, CompanyName = "Tata",
                Mrp = 28.0m, SalesRate1 = 26.0m, SalesRate2 = 25.0m, MinimumQty = 10, MaximumQty = 100, ShelfLife = 1095,
                MaximumDiscount = 0.0m, DecimalAllowed = false, Conversion = 1, Salt = ""
            },
            new SeedItemDto
            {
                Name = "Wheat Flour 5kg", Code = "GROC005", Barcode = "9876543210127", Unit = "Kg", Packing = "5kg Bag",
                CategoryName = "Flours", SubCategoryName = "Atta", DivisionName = "Food Division", ShelfLifeUnit = "Days",
                HsnCode = "11010000", SGST = 0.0m, CGST = 0.0m, IGST = 0.0m, Cess = 0.0m, CompanyName = "Aashirvaad",
                Mrp = 290.0m, SalesRate1 = 270.0m, SalesRate2 = 265.0m, MinimumQty = 5, MaximumQty = 50, ShelfLife = 90,
                MaximumDiscount = 2.0m, DecimalAllowed = false, Conversion = 1, Salt = ""
            }
        };
    }
}
