IF OBJECT_ID(N'dbo.usp_ItemMaster_GetAll', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_ItemMaster_GetAll AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_ItemMaster_GetAll
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        im.Id,
        im.Name,
        im.Code,
        im.Barcode,
        im.Unit1,
        im.Unit2,
        im.Packing,
        im.CategoryId,
        im.DivisionId,
        im.HsnId,
        im.Mrp,
        im.SalesRate1,
        im.SalesRate2,
        im.MinimumQty,
        im.MaximumQty,
        im.ShelfLife,
        im.ShelfLifeUnit,
        im.MaximumDiscount,
        im.DecemalAllowed,
        im.Conversion,
        im.ItemType,
        im.ParentItemId,
        im.ConversionFactor,
        im.SubCategoryId,
        im.CompanyId,
        im.TenantId,
        im.UploadImage,
        im.Local,
        im.Central,
        im.IsActive,
        im.Narcotics,
        im.ScheduleH,
        im.ScheduleH1,
        im.Salt,
        im.Created,
        im.CreatedBy,
        im.LastModified,
        im.LastModifiedBy,
        im.Deleted,
        im.DeletedBy,
        cm.CategoryName,
        cm.TenantId AS CategoryTenantId,
        sc.Name AS SubCategoryName,
        sc.CategoryId AS SubCategoryCategoryId,
        sc.TenantId AS SubCategoryTenantId,
        co.Name AS CompanyName,
        co.TenantId AS CompanyTenantId,
        d.Name AS DivisionName,
        d.CompanyId AS DivisionCompanyId,
        d.TenantId AS DivisionTenantId,
        h.HsnCode,
        h.SGST,
        h.CGST,
        h.IGST,
        h.Cess,
        h.HsnType,
        h.TenantId AS HsnTenantId
    FROM dbo.ItemMasters im
    LEFT JOIN dbo.CategoryMasters cm
        ON cm.Id = im.CategoryId
       AND cm.Deleted IS NULL
    LEFT JOIN dbo.SubCategories sc
        ON sc.Id = im.SubCategoryId
       AND sc.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Divisions d
        ON d.Id = im.DivisionId
       AND d.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE im.Deleted IS NULL
      AND
      (
          @FilterMode IS NULL
          OR @FilterMode = N'All'
          OR (@FilterMode IN (N'Tenant', N'User') AND (
              im.TenantId = @TenantId
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.IsHeadOffice = 1)
                  AND im.TenantId IN (SELECT id FROM dbo.Tenants WHERE ParentTenantId = @TenantId)
              )
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.ParentTenantId IS NOT NULL)
                  AND EXISTS (SELECT 1 FROM dbo.BranchItemMappings bim WHERE bim.TenantId = @TenantId AND bim.ItemMasterId = im.Id AND bim.IsActive = 1 AND bim.Deleted IS NULL)
              )
          ))
          
      )
    ORDER BY im.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_ItemMaster_GetById', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_ItemMaster_GetById AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_ItemMaster_GetById
    @Id INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        im.Id,
        im.Name,
        im.Code,
        im.Barcode,
        im.Unit1,
        im.Unit2,
        im.Packing,
        im.CategoryId,
        im.DivisionId,
        im.HsnId,
        im.Mrp,
        im.SalesRate1,
        im.SalesRate2,
        im.MinimumQty,
        im.MaximumQty,
        im.ShelfLife,
        im.ShelfLifeUnit,
        im.MaximumDiscount,
        im.DecemalAllowed,
        im.Conversion,
        im.ItemType,
        im.ParentItemId,
        im.ConversionFactor,
        im.SubCategoryId,
        im.CompanyId,
        im.TenantId,
        im.UploadImage,
        im.Local,
        im.Central,
        im.IsActive,
        im.Narcotics,
        im.ScheduleH,
        im.ScheduleH1,
        im.Salt,
        im.Created,
        im.CreatedBy,
        im.LastModified,
        im.LastModifiedBy,
        im.Deleted,
        im.DeletedBy,
        cm.CategoryName,
        cm.TenantId AS CategoryTenantId,
        sc.Name AS SubCategoryName,
        sc.CategoryId AS SubCategoryCategoryId,
        sc.TenantId AS SubCategoryTenantId,
        co.Name AS CompanyName,
        co.TenantId AS CompanyTenantId,
        d.Name AS DivisionName,
        d.CompanyId AS DivisionCompanyId,
        d.TenantId AS DivisionTenantId,
        h.HsnCode,
        h.SGST,
        h.CGST,
        h.IGST,
        h.Cess,
        h.HsnType,
        h.TenantId AS HsnTenantId
    FROM dbo.ItemMasters im
    LEFT JOIN dbo.CategoryMasters cm
        ON cm.Id = im.CategoryId
       AND cm.Deleted IS NULL
    LEFT JOIN dbo.SubCategories sc
        ON sc.Id = im.SubCategoryId
       AND sc.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Divisions d
        ON d.Id = im.DivisionId
       AND d.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE im.Deleted IS NULL
      AND im.Id = @Id
      AND
      (
          @FilterMode IS NULL
          OR @FilterMode = N'All'
          OR (@FilterMode IN (N'Tenant', N'User') AND (
              im.TenantId = @TenantId
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.IsHeadOffice = 1)
                  AND im.TenantId IN (SELECT id FROM dbo.Tenants WHERE ParentTenantId = @TenantId)
              )
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.ParentTenantId IS NOT NULL)
                  AND EXISTS (SELECT 1 FROM dbo.BranchItemMappings bim WHERE bim.TenantId = @TenantId AND bim.ItemMasterId = im.Id AND bim.IsActive = 1 AND bim.Deleted IS NULL)
              )
          ))
          
      )
    ORDER BY im.Id;

    SELECT
        ii.Id,
        ii.ItemMasterId,
        ii.ImagePath,
        ii.ImageHash,
        ii.IsPrimary,
        ii.SortOrder,
        ii.IsDeleted,
        ii.TenantId,
        ii.Created,
        ii.CreatedBy,
        ii.LastModified,
        ii.LastModifiedBy,
        ii.Deleted,
        ii.DeletedBy
    FROM dbo.ItemImages ii
    WHERE ii.Deleted IS NULL
      AND ii.ItemMasterId = @Id
    ORDER BY ii.SortOrder, ii.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_ItemMaster_GetByBarcode', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_ItemMaster_GetByBarcode AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_ItemMaster_GetByBarcode
    @Barcode NVARCHAR(100) = NULL,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        im.Id,
        im.Name,
        im.Code,
        im.Barcode,
        im.Unit1,
        im.Unit2,
        im.Packing,
        im.CategoryId,
        im.DivisionId,
        im.HsnId,
        im.Mrp,
        im.SalesRate1,
        im.SalesRate2,
        im.MinimumQty,
        im.MaximumQty,
        im.ShelfLife,
        im.ShelfLifeUnit,
        im.MaximumDiscount,
        im.DecemalAllowed,
        im.Conversion,
        im.ItemType,
        im.ParentItemId,
        im.ConversionFactor,
        im.SubCategoryId,
        im.CompanyId,
        im.TenantId,
        im.UploadImage,
        im.Local,
        im.Central,
        im.IsActive,
        im.Narcotics,
        im.ScheduleH,
        im.ScheduleH1,
        im.Salt,
        im.Created,
        im.CreatedBy,
        im.LastModified,
        im.LastModifiedBy,
        im.Deleted,
        im.DeletedBy,
        cm.CategoryName,
        cm.TenantId AS CategoryTenantId,
        sc.Name AS SubCategoryName,
        sc.CategoryId AS SubCategoryCategoryId,
        sc.TenantId AS SubCategoryTenantId,
        co.Name AS CompanyName,
        co.TenantId AS CompanyTenantId,
        d.Name AS DivisionName,
        d.CompanyId AS DivisionCompanyId,
        d.TenantId AS DivisionTenantId,
        h.HsnCode,
        h.SGST,
        h.CGST,
        h.IGST,
        h.Cess,
        h.HsnType,
        h.TenantId AS HsnTenantId
    FROM dbo.ItemMasters im
    LEFT JOIN dbo.CategoryMasters cm
        ON cm.Id = im.CategoryId
       AND cm.Deleted IS NULL
    LEFT JOIN dbo.SubCategories sc
        ON sc.Id = im.SubCategoryId
       AND sc.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Divisions d
        ON d.Id = im.DivisionId
       AND d.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE im.Deleted IS NULL
      AND im.Barcode = LTRIM(RTRIM(@Barcode))
      AND
      (
          @FilterMode IS NULL
          OR @FilterMode = N'All'
          OR (@FilterMode IN (N'Tenant', N'User') AND (
              im.TenantId = @TenantId
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.IsHeadOffice = 1)
                  AND im.TenantId IN (SELECT id FROM dbo.Tenants WHERE ParentTenantId = @TenantId)
              )
              OR (
                  EXISTS (SELECT 1 FROM dbo.Tenants t WHERE t.Id = @TenantId AND t.ParentTenantId IS NOT NULL)
                  AND EXISTS (SELECT 1 FROM dbo.BranchItemMappings bim WHERE bim.TenantId = @TenantId AND bim.ItemMasterId = im.Id AND bim.IsActive = 1 AND bim.Deleted IS NULL)
              )
          ))
          
      )
    ORDER BY im.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_ItemImage_GetAll', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_ItemImage_GetAll AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE [dbo].[usp_ItemImage_GetAll]
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ItemMasterId,
        ImagePath
    FROM dbo.ItemImages
    WHERE Deleted IS NULL
      AND IsDeleted = 0
      AND
      (
          @FilterMode IS NULL
          OR @FilterMode = N'All'
          OR (@FilterMode = N'Tenant' AND TenantId = @TenantId)
          OR (@FilterMode = N'User' AND CreatedBy = @UserId)
      )
    ORDER BY ItemMasterId, SortOrder;
END
GO
