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
          OR (@FilterMode = N'Tenant' AND im.TenantId = @TenantId)
          OR (@FilterMode = N'User' AND im.CreatedBy = @UserId)
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
          OR (@FilterMode = N'Tenant' AND im.TenantId = @TenantId)
          OR (@FilterMode = N'User' AND im.CreatedBy = @UserId)
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
          OR (@FilterMode = N'Tenant' AND im.TenantId = @TenantId)
          OR (@FilterMode = N'User' AND im.CreatedBy = @UserId)
      )
    ORDER BY im.Id;
END
GO
