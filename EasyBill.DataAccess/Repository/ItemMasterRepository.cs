using AOne.DataAccess.Repository;
using AOne.DataAccess.Data;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models.Entity;
using AOne.Utility.Enums;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.Models.Entity;
using EasyBill.Models.Model.Response;
using EasyBill.Models.ViewModels;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyBill.DataAccess.Repository
{
    public class ItemMasterRepository : StoredProcedureRepositoryBase, IItemMasterRepository
    {
        private readonly IUnitOfWork _unitofwork;
        public ItemMasterRepository(
            IUnitOfWork unitofwork,
            ApplicationDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ITenantAccessor tenantAccessor)
            : base(dbContext, httpContextAccessor, tenantAccessor)
        {
            _unitofwork = unitofwork;
        }
        public async Task<IList<ItemMaster>> GetAll()
        {
            // Previous EF implementation kept commented for reference, as requested.
            // try
            // {
            //     var repository = _unitofwork.GetRepository<ItemMaster>();
            //     IList<ItemMaster> results = await repository.Query().Include(x => x.Category).Include(x => x.SubCategory).Include(x => x.Company).Include(x => x.Hsn).Include(x => x.Division).ToListAsync();
            //     return results;
            // }
            // catch (Exception ex)
            // {
            //     throw ex;
            // }

            return await WithStoredProcedureCommandAsync("dbo.usp_ItemMaster_GetAll", async command =>
            {
                AddFilterParameters(command);

                var results = new List<ItemMaster>();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(MapItemMaster(reader));
                }

                return (IList<ItemMaster>)results;
            });
        }
        public async Task<IList<CategoryItems>> GetAllCategoryItems()
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();

                var categoryItems = await repository.Query().Where(x => x.CategoryId != null && x.CategoryId != 0)
                    .Select(x => new CategoryItems
                    {
                        //Id = x.Id,
                        CategoryId = x.CategoryId ?? 0,
                        CategoryName = x.Category.CategoryName ?? ""
                    }).Distinct()
                    .ToListAsync();

                return categoryItems;
            }
            catch
            {
                throw;
            }
        }

        public async Task<IList<ItemMaster>> GetItemsByCategory(int categoryId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                IList<ItemMaster> results = await repository.Query().Where(x => x.CategoryId == categoryId).Include(x => x.Category).Include(x => x.SubCategory).Include(x => x.Company).Include(x => x.Hsn).Include(x => x.Division).ToListAsync();

                return results;
            }
            catch
            {
                throw;
            }
        }
        public async Task<IList<ItemSearchDto>> GetItemsBySearch(string query)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                var results = await repository.Query()
                        .Where(x =>
                            x.IsActive &&
                            (
                                x.Name.ToLower().Contains(query) || x.Category.CategoryName.ToLower().Contains(query) || x.SubCategory.Name.ToLower().Contains(query)
                            )
                        )
                        .Include(x => x.Category)
                        .Include(x => x.SubCategory)
                        .OrderBy(x => x.Name)
                        .Take(8) // 🔥 LIMIT for header search
                        .Select(x => new ItemSearchDto
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Category = x.Category.CategoryName,
                            SubCategory = x.SubCategory.Name,
                            ImageUrl = x.UploadImage
                        })
                        .ToListAsync();

                return results;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<OrderResponse>> GetOrdersByCustomer(int customerId)
        {
            var orderRepo = _unitofwork.GetRepository<SalesOrder>();
            var orders = await _unitofwork
        .GetRepository<SalesOrder>()
        .Query()
        .Where(o => o.CustomerId == customerId)
        .Include(o => o.salesOrderItems)
            .ThenInclude(i => i.ItemMaster)
        .Include(o => o.SalsePaymentDetails)
            .ThenInclude(p => p.ModeOfPayment)
        .OrderByDescending(o => o.BillDate)
        .Select(o => new OrderResponse
        {
            Id = o.Id,
            OrderNumber = o.BillNo,
            Date = o.BillDate ,//?? DateTime.Now, 
            Status = "Delivered", 
            TotalAmount = o.PaidAmount,
            DeliveryAddress = o.Address,

            PaymentMethod = o.SalsePaymentDetails
                .Select(p => p.ModeOfPayment.Name)
                .FirstOrDefault(),

            Items = o.salesOrderItems.Select(i => new OrderItemResponse
            {
                ProductId = i.ItemMasterId,
                ProductName = i.ItemMaster.Name,
                Quantity = (int)i.Qty, 
                Price = i.ItemMaster.Mrp,
                Image = i.ItemMaster.UploadImage
            }).ToList()
        })
        .ToListAsync();


            //var orders = await orderRepo
            //    .Query()
            //    .Where(o => o.CustomerId == customerId)
            //    .Include(o => o.salesOrderItems)
            //        .ThenInclude(i => i.ItemMaster)
            //    .Include(o => o.SalsePaymentDetails)
            //        .ThenInclude(p => p.ModeOfPayment)
            //    .OrderByDescending(o => o.BillDate)
            //    .Select(o => new OrderResponse
            //    {
            //        Id = o.Id,
            //        OrderNumber = o.BillNo,
            //        Date = o.BillDate ?? DateTime.MinValue,   // null-safe
            //        //Status = o.IsCancelled ? "Cancelled" : "Delivered",
            //        Status = "Delivered",
            //        TotalAmount = o.PaidAmount,
            //        DeliveryAddress = o.Address,

            //        PaymentMethod = o.SalsePaymentDetails
            //            .Select(p => p.ModeOfPayment.Name)
            //            .FirstOrDefault(),

            //        Items = o.salesOrderItems.Select(i => new OrderItemResponse
            //        {
            //            ProductId = i.ItemMasterId,
            //            ProductName = i.ItemMaster.Name,
            //            Quantity = i.salesOrderItems.Qty,
            //            Price = i.ItemMaster.Mrp,
            //            Image = i.ItemMaster.UploadImage
            //        }).ToList()
            //    })
            //    .ToListAsync();

            return orders;
        }

        public async Task<ItemMaster> GetProductById(int Id)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                ItemMaster results = await repository.Query().Where(x => x.Id == Id).Include(x => x.Category).Include(x => x.SubCategory).Include(x => x.Company).Include(x => x.Hsn).Include(x => x.Division).FirstOrDefaultAsync();

                return results;
            }
            catch
            {
                throw;
            }
        }

        public async Task<ItemMaster> Create(ItemMaster model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                model.IsActive = model.IsActive;
                repository.Add(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }
                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task ItemAddRange(List<ItemMaster> items)
        {
            try
            {
                var itemRepo = _unitofwork.GetRepository<ItemMaster>();
                using (var transaction = itemRepo.BeginTransaction())
                {
                    if (items != null && items.Count > 0)
                    {
                        itemRepo.AddRange(items);
                        await itemRepo.SaveChangesAsync();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("CreateWithitemsAsync failed: " + ex.Message);
            }
        }
        public async Task<ItemMaster> GetByItemMasterId(int? Id)
        {
            // Previous EF implementation kept commented for reference, as requested.
            // try
            // {
            //     var repository = _unitofwork.GetRepository<ItemMaster>();
            //     var result = await repository.Query().Include(l => l.Category).Include(l => l.Hsn).Include(l => l.ItemImages).Where(l => l.Id == Id).FirstOrDefaultAsync();
            //     return result;
            // }
            // catch (Exception ex)
            // {
            //     throw ex;
            // }

            if (!Id.HasValue)
            {
                return null!;
            }

            return await WithStoredProcedureCommandAsync("dbo.usp_ItemMaster_GetById", async command =>
            {
                AddParameter(command, "@Id", Id.Value, DbType.Int32);
                AddFilterParameters(command);

                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null!;
                }

                var item = MapItemMaster(reader);

                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        item.ItemImages.Add(MapItemImage(reader, item));
                    }
                }

                return item;
            });
        }
        public async Task<ItemMaster> GetByBarcode(string? barcode)
        {
            // Previous EF implementation kept commented for reference, as requested.
            // try
            // {
            //     var repository = _unitofwork.GetRepository<ItemMaster>();
            //     var result = await repository.Query().Include(l => l.Category).Include(l => l.Hsn).Where(l => l.Barcode == barcode).FirstOrDefaultAsync();
            //     return result;
            // }
            // catch (Exception ex)
            // {
            //     throw ex;
            // }

            return await WithStoredProcedureCommandAsync("dbo.usp_ItemMaster_GetByBarcode", async command =>
            {
                AddParameter(command, "@Barcode", string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim());
                AddFilterParameters(command);

                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null!;
                }

                return MapItemMaster(reader);
            });
        }

        public async Task<bool> ExistsByCode(string code)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                var result = await repository.Query().AnyAsync(x => x.Code == code);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<ItemMaster> Update(ItemMaster model)
        {
            try
            {
                var repository = _unitofwork.GetRepository<ItemMaster>();
                repository.Update(model);
                using (var transaction = repository.BeginTransaction())
                {
                    await repository.SaveChangesAsync();
                    transaction.Commit();
                }

                return model;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task Delete(ItemMaster model)
        {
            try
            {
                var assetGroupRepository = _unitofwork.GetRepository<ItemMaster>();
                assetGroupRepository.Delete(model);
                using (var transaction = assetGroupRepository.BeginTransaction())
                {
                    await assetGroupRepository.SaveChangesAsync();
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> CheckDuplicateAsync(string code, int id)
        {
            var repo = _unitofwork.GetRepository<ItemMaster>();

            bool alreadyExists = await repo.Query()
                .AnyAsync(x =>
                    x.Code == code &&
                    x.Id != id
                );

            return alreadyExists;
        }

        public async Task<int> HasAnySaleAsync(int itemMasterId, string tenantId)
        {
            try
            {
                var repository = _unitofwork.GetRepository<SalesItem>();

                return await repository.Query()
                    .Where(x =>
                        x.ItemMasterId == itemMasterId &&
                        x.TenantId == tenantId &&
                        x.Deleted == null)
                    .CountAsync();
            }
            catch
            {
                throw; // ✔ correct way
            }
        }

        private static ItemMaster MapItemMaster(DbDataReader reader)
        {
            var item = new ItemMaster
            {
                Id = reader.ReadInt32("Id"),
                Name = reader.ReadNullableString("Name") ?? string.Empty,
                Code = reader.ReadNullableString("Code") ?? string.Empty,
                Barcode = reader.ReadNullableString("Barcode"),
                Unit1 = reader.ReadNullableString("Unit1"),
                Unit2 = reader.ReadNullableString("Unit2"),
                Packing = reader.ReadNullableString("Packing"),
                CategoryId = reader.ReadNullableInt32("CategoryId"),
                DivisionId = reader.ReadNullableInt32("DivisionId"),
                HsnId = reader.ReadNullableInt32("HsnId"),
                Mrp = reader.ReadDecimal("Mrp"),
                SalesRate1 = reader.ReadDecimal("SalesRate1"),
                SalesRate2 = reader.ReadDecimal("SalesRate2"),
                MinimumQty = reader.ReadInt32("MinimumQty"),
                MaximumQty = reader.ReadInt32("MaximumQty"),
                ShelfLife = reader.ReadInt32("ShelfLife"),
                ShelfLifeUnit = reader.ReadNullableString("ShelfLifeUnit"),
                MaximumDiscount = reader.ReadDecimal("MaximumDiscount"),
                DecemalAllowed = ReadBoolean(reader, "DecemalAllowed"),
                Conversion = reader.ReadInt32("Conversion"),
                ItemType = reader.HasColumn("ItemType") ? reader.ReadNullableString("ItemType") : null,
                ParentItemId = reader.HasColumn("ParentItemId") ? reader.ReadNullableInt32("ParentItemId") : null,
                ConversionFactor = reader.HasColumn("ConversionFactor") ? reader.ReadNullableDecimal("ConversionFactor") : null,
                SubCategoryId = reader.ReadNullableInt32("SubCategoryId"),
                CompanyId = reader.ReadNullableInt32("CompanyId"),
                TenantId = reader.ReadNullableString("TenantId"),
                UploadImage = reader.ReadNullableString("UploadImage"),
                Local = ReadNullableEnum<TaxStatus>(reader, "Local"),
                Central = ReadNullableEnum<TaxStatus>(reader, "Central"),
                IsActive = ReadBoolean(reader, "IsActive"),
                Narcotics = ReadBoolean(reader, "Narcotics"),
                ScheduleH = ReadBoolean(reader, "ScheduleH"),
                ScheduleH1 = ReadBoolean(reader, "ScheduleH1"),
                Salt = reader.ReadNullableString("Salt"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };

            if (item.CategoryId.HasValue)
            {
                item.Category = new CategoryMaster
                {
                    Id = item.CategoryId.Value,
                    CategoryName = reader.ReadNullableString("CategoryName") ?? string.Empty,
                    TenantId = reader.ReadNullableString("CategoryTenantId")
                };
            }

            if (item.SubCategoryId.HasValue)
            {
                item.SubCategory = new SubCategory
                {
                    Id = item.SubCategoryId.Value,
                    Name = reader.ReadNullableString("SubCategoryName") ?? string.Empty,
                    CategoryId = reader.ReadNullableInt32("SubCategoryCategoryId") ?? 0,
                    TenantId = reader.ReadNullableString("SubCategoryTenantId")
                };
            }

            if (item.CompanyId.HasValue)
            {
                item.Company = new Company
                {
                    Id = item.CompanyId.Value,
                    Name = reader.ReadNullableString("CompanyName") ?? string.Empty,
                    TenantId = reader.ReadNullableString("CompanyTenantId")
                };
            }

            if (item.DivisionId.HasValue)
            {
                item.Division = new Division
                {
                    Id = item.DivisionId.Value,
                    Name = reader.ReadNullableString("DivisionName") ?? string.Empty,
                    CompanyId = reader.ReadNullableInt32("DivisionCompanyId") ?? 0,
                    TenantId = reader.ReadNullableString("DivisionTenantId")
                };
            }

            if (item.HsnId.HasValue)
            {
                item.Hsn = new Hsn
                {
                    Id = item.HsnId.Value,
                    HsnCode = reader.ReadNullableString("HsnCode") ?? string.Empty,
                    SGST = reader.ReadDecimal("SGST"),
                    CGST = reader.ReadDecimal("CGST"),
                    IGST = reader.ReadDecimal("IGST"),
                    Cess = reader.ReadDecimal("Cess"),
                    HsnType = ReadNullableEnum<HsnType>(reader, "HsnType"),
                    TenantId = reader.ReadNullableString("HsnTenantId")
                };
            }

            return item;
        }

        private static ItemImage MapItemImage(DbDataReader reader, ItemMaster itemMaster)
        {
            return new ItemImage
            {
                Id = reader.ReadInt32("Id"),
                ItemMasterId = itemMaster.Id,
                ItemMaster = itemMaster,
                ImagePath = reader.ReadNullableString("ImagePath") ?? string.Empty,
                ImageHash = reader.ReadNullableString("ImageHash") ?? string.Empty,
                IsPrimary = ReadBoolean(reader, "IsPrimary"),
                SortOrder = reader.ReadInt32("SortOrder"),
                IsDeleted = ReadBoolean(reader, "IsDeleted"),
                TenantId = reader.ReadNullableString("TenantId"),
                Created = reader.ReadNullableDateTime("Created"),
                CreatedBy = reader.ReadNullableString("CreatedBy"),
                LastModified = reader.ReadNullableDateTime("LastModified"),
                LastModifiedBy = reader.ReadNullableString("LastModifiedBy"),
                Deleted = reader.ReadNullableDateTime("Deleted"),
                DeletedBy = reader.ReadNullableString("DeletedBy")
            };
        }

        private static bool ReadBoolean(DbDataReader reader, string columnName)
        {
            if (!reader.HasColumn(columnName))
            {
                return false;
            }

            var value = reader[columnName];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static TEnum? ReadNullableEnum<TEnum>(DbDataReader reader, string columnName)
            where TEnum : struct, Enum
        {
            var value = reader.ReadNullableInt32(columnName);
            return value.HasValue ? (TEnum?)Enum.ToObject(typeof(TEnum), value.Value) : null;
        }
    }
}




