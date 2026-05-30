IF OBJECT_ID(N'dbo.usp_Dashboard_LoadHomeIndexRest', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Dashboard_LoadHomeIndexRest AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Dashboard_LoadHomeIndexRest
    @StartDate DATE,
    @EndDate   DATE,
    @FilterMode NVARCHAR(20) = N'All',
    @TenantId  NVARCHAR(450) = NULL,
    @UserId    NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @IsAll BIT = CASE WHEN @FilterMode IS NULL OR @FilterMode = N'All' THEN 1 ELSE 0 END;
    DECLARE @IsTenant BIT = CASE WHEN @FilterMode = N'Tenant' THEN 1 ELSE 0 END;
    DECLARE @IsUser BIT = CASE WHEN @FilterMode = N'User' THEN 1 ELSE 0 END;

    SELECT
        pc.Id, pc.SupplierId, pc.BillNo, pc.BillDate, pc.PartyBillNo, pc.PartyBillDate,
        pc.TenantId, pc.TotalGstAmt, pc.Totaldiscount, pc.TotalPayable, pc.discountPercent,
        pc.discountAmount, pc.Total, pc.PaymentAmt, pc.PaymentStatus, pc.RoundOffAmount,
        pc.billingType, pc.PaymentType, pc.PurchaseType, pc.TotalCGstAmt, pc.TotalSGstAmt,
        pc.PaidAmount, pc.ReturnAmount, pc.Balance, pc.Status, pc.ConvertedPurchaseId,
        pc.Created, pc.CreatedBy, pc.LastModified, pc.LastModifiedBy, pc.Deleted, pc.DeletedBy,
        sup.FirstName AS SupplierFirstName,
        sup.PhoneNO AS SupplierPhoneNO
    FROM dbo.PurchaseChallans pc
    LEFT JOIN dbo.Suppliers sup ON sup.Id = pc.SupplierId AND sup.Deleted IS NULL
    WHERE pc.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND pc.TenantId = @TenantId)
            OR (@IsUser = 1 AND pc.CreatedBy = @UserId)
      );

    SELECT pci.*
    FROM dbo.PurchaseChallanItems pci
    INNER JOIN dbo.PurchaseChallans pc ON pc.Id = pci.PurchaseChallanId AND pc.Deleted IS NULL
    WHERE pci.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND pc.TenantId = @TenantId)
            OR (@IsUser = 1 AND pc.CreatedBy = @UserId)
      );

    SELECT spd.Id, spd.PaymentModeId, spd.Amount, spd.ReferenceNo, spd.Description, spd.CustomerId,
           spd.SalseId, spd.TenantId, spd.SalesOrderId, spd.Date, spd.StockReturnId, spd.StockIssueId,
           spd.PurchaseId, spd.PurchaseChallanId, spd.Created, spd.CreatedBy, spd.LastModified,
           spd.LastModifiedBy, spd.Deleted, spd.DeletedBy,
           mop.Name AS ModeOfPaymentName
    FROM dbo.SalsePaymentDetails spd
    INNER JOIN dbo.ModeOfPayments mop ON mop.Id = spd.PaymentModeId AND mop.Deleted IS NULL
    WHERE spd.Deleted IS NULL
      AND spd.PurchaseChallanId IS NOT NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND spd.TenantId = @TenantId)
            OR (@IsUser = 1 AND spd.CreatedBy = @UserId)
      );

    SELECT
        pv.Id, pv.VouncherNo, pv.Date, pv.SupplierId, pv.VoucherCategoryId, pv.Amount, pv.GST,
        pv.GSTAmount, pv.NetAmount, pv.Description, pv.Attachments, pv.ChequeNo, pv.ChequeDate,
        pv.RefNo, pv.CustomerId, pv.EmployeeId, pv.Party, pv.PaymentModeId,
        pv.Created, pv.CreatedBy, pv.LastModified, pv.LastModifiedBy, pv.Deleted, pv.DeletedBy,
        mop.Name AS ModeOfPaymentName,
        sup.FirstName AS SupplierFirstName,
        sup.PhoneNO AS SupplierPhoneNO,
        cat.Name AS VoucherCategoryName
    FROM dbo.PaymentVoucher pv
    LEFT JOIN dbo.ModeOfPayments mop ON mop.Id = pv.PaymentModeId AND mop.Deleted IS NULL
    LEFT JOIN dbo.Suppliers sup ON sup.Id = pv.SupplierId AND sup.Deleted IS NULL
    LEFT JOIN dbo.PaymentVoucherCategories cat ON cat.Id = pv.VoucherCategoryId AND cat.Deleted IS NULL
    LEFT JOIN dbo.AspNetUsers au ON au.Id = pv.CreatedBy
    WHERE pv.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND au.TenantId = @TenantId)
            OR (@IsUser = 1 AND pv.CreatedBy = @UserId)
      );

    SELECT
        pr.Id, pr.SupplierId, pr.BillNo, pr.BillDate, pr.PartyBillNo, pr.PartyBillDate, pr.TenantId,
        pr.TotalGstAmt, pr.Totaldiscount, pr.TotalPayable, pr.discountPercent, pr.discountAmount,
        pr.Total, pr.RoundOffAmount, pr.TotalCessAmt, pr.Reason,
        pr.Created, pr.CreatedBy, pr.LastModified, pr.LastModifiedBy, pr.Deleted, pr.DeletedBy,
        sup.FirstName AS SupplierFirstName,
        sup.PhoneNO AS SupplierPhoneNO
    FROM dbo.PurchaseReturns pr
    LEFT JOIN dbo.Suppliers sup ON sup.Id = pr.SupplierId AND sup.Deleted IS NULL
    WHERE pr.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND pr.TenantId = @TenantId)
            OR (@IsUser = 1 AND pr.CreatedBy = @UserId)
      )
      AND pr.BillDate IS NOT NULL
      AND CAST(pr.BillDate AS DATE) >= @StartDate
      AND CAST(pr.BillDate AS DATE) <= @EndDate;

    SELECT pri.*
    FROM dbo.PurchaseReturnItems pri
    INNER JOIN dbo.PurchaseReturns pr ON pr.Id = pri.PurchaseReturnId AND pr.Deleted IS NULL
    WHERE pri.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND pr.TenantId = @TenantId)
            OR (@IsUser = 1 AND pr.CreatedBy = @UserId)
      )
      AND pr.BillDate IS NOT NULL
      AND CAST(pr.BillDate AS DATE) >= @StartDate
      AND CAST(pr.BillDate AS DATE) <= @EndDate;

    SELECT
        si.Id, si.CustomerId, si.billingType, si.PaymentType, si.ChallanNo, si.ChallanDate,
        si.MobileNo, si.Address, si.Total, si.TotalGstAmt, si.TotalPayable, si.TenantId,
        si.PharmacyDoctorId, si.DoctorMobileNumber, si.DoctorRegNumber, si.Totaldiscount,
        si.discountPercent, si.discountAmount, si.PaidAmount, si.ReturnAmount, si.Balance,
        si.NetCollection, si.RoundOffAmount, si.TotalCessAmount, si.TaxCalculation,
        si.Created, si.CreatedBy, si.LastModified, si.LastModifiedBy, si.Deleted, si.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo
    FROM dbo.StockIssues si
    LEFT JOIN dbo.Customers c ON c.Id = si.CustomerId AND c.Deleted IS NULL
    WHERE si.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND si.TenantId = @TenantId)
            OR (@IsUser = 1 AND si.CreatedBy = @UserId)
      )
      AND si.ChallanDate IS NOT NULL
      AND CAST(si.ChallanDate AS DATE) >= @StartDate
      AND CAST(si.ChallanDate AS DATE) <= @EndDate;

    SELECT sii.*
    FROM dbo.StockIssueItems sii
    INNER JOIN dbo.StockIssues si ON si.Id = sii.StockIssueId AND si.Deleted IS NULL
    WHERE sii.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND si.TenantId = @TenantId)
            OR (@IsUser = 1 AND si.CreatedBy = @UserId)
      )
      AND si.ChallanDate IS NOT NULL
      AND CAST(si.ChallanDate AS DATE) >= @StartDate
      AND CAST(si.ChallanDate AS DATE) <= @EndDate;

    SELECT spd.Id, spd.PaymentModeId, spd.Amount, spd.ReferenceNo, spd.Description, spd.CustomerId,
           spd.SalseId, spd.TenantId, spd.SalesOrderId, spd.Date, spd.StockReturnId, spd.StockIssueId,
           spd.PurchaseId, spd.PurchaseChallanId, spd.Created, spd.CreatedBy, spd.LastModified,
           spd.LastModifiedBy, spd.Deleted, spd.DeletedBy,
           mop.Name AS ModeOfPaymentName
    FROM dbo.SalsePaymentDetails spd
    INNER JOIN dbo.ModeOfPayments mop ON mop.Id = spd.PaymentModeId AND mop.Deleted IS NULL
    INNER JOIN dbo.StockIssues si ON si.Id = spd.StockIssueId AND si.Deleted IS NULL
    WHERE spd.Deleted IS NULL
      AND spd.StockIssueId IS NOT NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND si.TenantId = @TenantId)
            OR (@IsUser = 1 AND si.CreatedBy = @UserId)
      )
      AND si.ChallanDate IS NOT NULL
      AND CAST(si.ChallanDate AS DATE) >= @StartDate
      AND CAST(si.ChallanDate AS DATE) <= @EndDate;

    SELECT
        sr.Id, sr.CustomerId, sr.ChallanNo, sr.ChallanDate, sr.MobileNo, sr.Address, sr.Total,
        sr.TotalGstAmt, sr.TotalPayable, sr.PharmacyDoctorId, sr.DoctorMobileNumber, sr.DoctorRegNumber,
        sr.Totaldiscount, sr.discountPercent, sr.discountAmount, sr.TenantId, sr.billingType,
        sr.PaymentType, sr.NetCollection, sr.RoundOffAmount, sr.TotalCessAmt,
        sr.Created, sr.CreatedBy, sr.LastModified, sr.LastModifiedBy, sr.Deleted, sr.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo
    FROM dbo.StockReturns sr
    LEFT JOIN dbo.Customers c ON c.Id = sr.CustomerId AND c.Deleted IS NULL
    WHERE sr.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      )
      AND sr.ChallanDate IS NOT NULL
      AND CAST(sr.ChallanDate AS DATE) >= @StartDate
      AND CAST(sr.ChallanDate AS DATE) <= @EndDate;

    SELECT sri.*
    FROM dbo.StockReturnItems sri
    INNER JOIN dbo.StockReturns sr ON sr.Id = sri.StockReturnId AND sr.Deleted IS NULL
    WHERE sri.Deleted IS NULL 
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      )
      AND sr.ChallanDate IS NOT NULL
      AND CAST(sr.ChallanDate AS DATE) >= @StartDate
      AND CAST(sr.ChallanDate AS DATE) <= @EndDate;

    SELECT
        sr.Id, sr.CustomerId, sr.ChallanNo, sr.ChallanDate, sr.MobileNo, sr.Address, sr.Total,
        sr.TotalGstAmt, sr.TotalPayable, sr.PharmacyDoctorId, sr.DoctorMobileNumber, sr.DoctorRegNumber,
        sr.Totaldiscount, sr.discountPercent, sr.discountAmount, sr.TenantId, sr.billingType,
        sr.PaymentType, sr.NetCollection, sr.RoundOffAmount, sr.TotalCessAmt,
        sr.Created, sr.CreatedBy, sr.LastModified, sr.LastModifiedBy, sr.Deleted, sr.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo
    FROM dbo.StockReturns sr
    LEFT JOIN dbo.Customers c ON c.Id = sr.CustomerId AND c.Deleted IS NULL
    WHERE sr.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      );

    SELECT sri.*
    FROM dbo.StockReturnItems sri
    INNER JOIN dbo.StockReturns sr ON sr.Id = sri.StockReturnId AND sr.Deleted IS NULL
    WHERE sri.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      );

    SELECT
        sr.Id, sr.CustomerId, sr.ChallanNo, sr.ChallanDate, sr.MobileNo, sr.Address, sr.Total,
        sr.TotalGstAmt, sr.TotalPayable, sr.TenantId, sr.PharmacyDoctorId, sr.DoctorMobileNumber,
        sr.DoctorRegNumber, sr.Totaldiscount, sr.discountPercent, sr.discountAmount,
        sr.Created, sr.CreatedBy, sr.LastModified, sr.LastModifiedBy, sr.Deleted, sr.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo
    FROM dbo.StockReceives sr
    LEFT JOIN dbo.Customers c ON c.Id = sr.CustomerId AND c.Deleted IS NULL
    WHERE sr.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      )
      AND sr.ChallanDate IS NOT NULL
      AND CAST(sr.ChallanDate AS DATE) >= @StartDate
      AND CAST(sr.ChallanDate AS DATE) <= @EndDate;

    SELECT sri.*
    FROM dbo.StockReceiveItems sri
    INNER JOIN dbo.StockReceives sr ON sr.Id = sri.StockReceiveId AND sr.Deleted IS NULL
    WHERE sri.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND sr.TenantId = @TenantId)
            OR (@IsUser = 1 AND sr.CreatedBy = @UserId)
      )
      AND sr.ChallanDate IS NOT NULL
      AND CAST(sr.ChallanDate AS DATE) >= @StartDate
      AND CAST(sr.ChallanDate AS DATE) <= @EndDate;

    SELECT cm.Id, cm.CategoryName, cm.TenantId, cm.Created, cm.CreatedBy, cm.LastModified,
           cm.LastModifiedBy, cm.Deleted, cm.DeletedBy
    FROM dbo.CategoryMasters cm
    WHERE cm.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND cm.TenantId = @TenantId)
            OR (@IsUser = 1 AND cm.CreatedBy = @UserId)
      );

    SELECT co.Id, co.Name, co.TenantId, co.Created, co.CreatedBy, co.LastModified,
           co.LastModifiedBy, co.Deleted, co.DeletedBy
    FROM dbo.Companies co
    WHERE co.Deleted IS NULL
      AND (
            @IsAll = 1
            OR (@IsTenant = 1 AND co.TenantId = @TenantId)
            OR (@IsUser = 1 AND co.CreatedBy = @UserId)
      );
END
GO
