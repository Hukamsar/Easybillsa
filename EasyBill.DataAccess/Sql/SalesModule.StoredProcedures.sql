IF TYPE_ID(N'dbo.SalesItemTvp') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.SalesItemTvp AS TABLE
        (
            Id INT NULL,
            ItemMasterId INT NOT NULL,
            PurchaseItemId INT NULL,
            Batch NVARCHAR(100) NULL,
            ExpiryDate DATETIME2 NULL,
            Mrp DECIMAL(18,2) NOT NULL,
            Qty DECIMAL(18,2) NOT NULL,
            Rate DECIMAL(18,2) NOT NULL,
            StripRate DECIMAL(18,2) NOT NULL,
            Gst DECIMAL(18,2) NOT NULL,
            Cess DECIMAL(18,2) NOT NULL,
            Discount DECIMAL(18,2) NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            IsSoldInTablets BIT NOT NULL
        );
    ');
END
GO

IF TYPE_ID(N'dbo.SalesPaymentDetailTvp') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.SalesPaymentDetailTvp AS TABLE
        (
            Id INT NULL,
            [Date] DATETIME2 NULL,
            PaymentModeId INT NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            ReferenceNo NVARCHAR(200) NULL,
            [Description] NVARCHAR(1000) NULL,
            CustomerId INT NULL
        );
    ');
END
GO

IF OBJECT_ID(N'dbo.usp_Sales_Save', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Sales_Save AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Sales_Save
    @Id INT = NULL,
    @CustomerId INT = NULL,
    @billingType NVARCHAR(MAX) = NULL,
    @PaymentType NVARCHAR(MAX) = NULL,
    @BillNo NVARCHAR(MAX) = NULL,
    @BillDate DATETIME2 = NULL,
    @MobileNo NVARCHAR(MAX) = NULL,
    @Address NVARCHAR(MAX) = NULL,
    @Total DECIMAL(18,2),
    @TotalGstAmt DECIMAL(18,2),
    @TotalPayable DECIMAL(18,2),
    @PharmacyDoctorId INT = NULL,
    @DoctorMobileNumber NVARCHAR(MAX) = NULL,
    @DoctorRegNumber NVARCHAR(MAX) = NULL,
    @Totaldiscount DECIMAL(18,2),
    @discountPercent DECIMAL(18,2),
    @discountAmount DECIMAL(18,2),
    @PaidAmount DECIMAL(18,2),
    @ReturnAmount DECIMAL(18,2),
    @OfferId INT = NULL,
    @SalesOrderId INT = NULL,
    @Balance DECIMAL(18,2),
    @NetCollection DECIMAL(18,2),
    @RoundOffAmount DECIMAL(18,2),
    @TotalCessAmount DECIMAL(18,2),
    @PaymentStatus NVARCHAR(MAX) = NULL,
    @SalesItems dbo.SalesItemTvp READONLY,
    @PaymentDetails dbo.SalesPaymentDetailTvp READONLY,
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @SalesId INT = NULLIF(@Id, 0);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @SalesId IS NULL
        BEGIN
            INSERT INTO dbo.Saless
            (
                CustomerId,
                billingType,
                PaymentType,
                BillNo,
                BillDate,
                MobileNo,
                Address,
                Total,
                TotalGstAmt,
                TotalPayable,
                PharmacyDoctorId,
                DoctorMobileNumber,
                DoctorRegNumber,
                Totaldiscount,
                discountPercent,
                discountAmount,
                PaidAmount,
                ReturnAmount,
                OfferId,
                SalesOrderId,
                Balance,
                NetCollection,
                RoundOffAmount,
                TotalCessAmount,
                PaymentStatus,
                TenantId,
                Created,
                CreatedBy
            )
            VALUES
            (
                @CustomerId,
                @billingType,
                @PaymentType,
                @BillNo,
                @BillDate,
                @MobileNo,
                @Address,
                @Total,
                @TotalGstAmt,
                @TotalPayable,
                @PharmacyDoctorId,
                @DoctorMobileNumber,
                @DoctorRegNumber,
                @Totaldiscount,
                @discountPercent,
                @discountAmount,
                @PaidAmount,
                @ReturnAmount,
                @OfferId,
                @SalesOrderId,
                @Balance,
                @NetCollection,
                @RoundOffAmount,
                @TotalCessAmount,
                @PaymentStatus,
                @TenantId,
                @Now,
                @UserId
            );

            SET @SalesId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE s
            SET
                CustomerId = @CustomerId,
                billingType = @billingType,
                PaymentType = @PaymentType,
                BillNo = @BillNo,
                BillDate = @BillDate,
                MobileNo = @MobileNo,
                Address = @Address,
                Total = @Total,
                TotalGstAmt = @TotalGstAmt,
                TotalPayable = @TotalPayable,
                PharmacyDoctorId = @PharmacyDoctorId,
                DoctorMobileNumber = @DoctorMobileNumber,
                DoctorRegNumber = @DoctorRegNumber,
                Totaldiscount = @Totaldiscount,
                discountPercent = @discountPercent,
                discountAmount = @discountAmount,
                PaidAmount = @PaidAmount,
                ReturnAmount = @ReturnAmount,
                OfferId = @OfferId,
                SalesOrderId = @SalesOrderId,
                Balance = @Balance,
                NetCollection = @NetCollection,
                RoundOffAmount = @RoundOffAmount,
                TotalCessAmount = @TotalCessAmount,
                PaymentStatus = @PaymentStatus,
                TenantId = COALESCE(s.TenantId, @TenantId),
                LastModified = @Now,
                LastModifiedBy = @UserId
            FROM dbo.Saless s
            WHERE s.Id = @SalesId
              AND s.Deleted IS NULL;

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR('Sale not found.', 16, 1);
                RETURN;
            END
        END

        UPDATE si
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.SalesItems si
        WHERE si.SalesId = @SalesId
          AND si.Deleted IS NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM @SalesItems src
              WHERE ISNULL(src.Id, 0) > 0
                AND src.Id = si.Id
          );

        UPDATE si
        SET
            ItemMasterId = src.ItemMasterId,
            PurchaseItemId = src.PurchaseItemId,
            Batch = src.Batch,
            Expirydate = src.ExpiryDate,
            Mrp = src.Mrp,
            Qty = src.Qty,
            Rate = src.Rate,
            StripRate = src.StripRate,
            Gst = src.Gst,
            Cess = src.Cess,
            Discount = src.Discount,
            Amount = src.Amount,
            IsSoldInTablets = src.IsSoldInTablets,
            TenantId = COALESCE(si.TenantId, @TenantId),
            LastModified = @Now,
            LastModifiedBy = @UserId
        FROM dbo.SalesItems si
        INNER JOIN @SalesItems src
            ON src.Id = si.Id
        WHERE si.SalesId = @SalesId
          AND si.Deleted IS NULL;

        INSERT INTO dbo.SalesItems
        (
            SalesId,
            ItemMasterId,
            PurchaseItemId,
            Batch,
            Expirydate,
            Mrp,
            Qty,
            Rate,
            StripRate,
            Gst,
            Cess,
            Discount,
            Amount,
            IsSoldInTablets,
            TenantId,
            Created,
            CreatedBy
        )
        SELECT
            @SalesId,
            src.ItemMasterId,
            src.PurchaseItemId,
            src.Batch,
            src.ExpiryDate,
            src.Mrp,
            src.Qty,
            src.Rate,
            src.StripRate,
            src.Gst,
            src.Cess,
            src.Discount,
            src.Amount,
            src.IsSoldInTablets,
            @TenantId,
            @Now,
            @UserId
        FROM @SalesItems src
        WHERE ISNULL(src.Id, 0) = 0;

        UPDATE pd
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.SalsePaymentDetails pd
        WHERE pd.SalseId = @SalesId
          AND pd.Deleted IS NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM @PaymentDetails src
              WHERE ISNULL(src.Id, 0) > 0
                AND src.Id = pd.Id
          );

        UPDATE pd
        SET
            [Date] = src.[Date],
            PaymentModeId = src.PaymentModeId,
            Amount = src.Amount,
            ReferenceNo = src.ReferenceNo,
            [Description] = src.[Description],
            CustomerId = src.CustomerId,
            TenantId = COALESCE(pd.TenantId, @TenantId),
            LastModified = @Now,
            LastModifiedBy = @UserId
        FROM dbo.SalsePaymentDetails pd
        INNER JOIN @PaymentDetails src
            ON src.Id = pd.Id
        WHERE pd.SalseId = @SalesId
          AND pd.Deleted IS NULL;

        INSERT INTO dbo.SalsePaymentDetails
        (
            PaymentModeId,
            Amount,
            ReferenceNo,
            [Description],
            CustomerId,
            SalseId,
            [Date],
            TenantId,
            Created,
            CreatedBy
        )
        SELECT
            src.PaymentModeId,
            src.Amount,
            src.ReferenceNo,
            src.[Description],
            src.CustomerId,
            @SalesId,
            src.[Date],
            @TenantId,
            @Now,
            @UserId
        FROM @PaymentDetails src
        WHERE ISNULL(src.Id, 0) = 0;

        COMMIT TRANSACTION;

        SELECT @SalesId AS SalesId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.usp_Sales_Delete', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Sales_Delete AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Sales_Delete
    @Id INT,
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE s
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.Saless s
        WHERE s.Id = @Id
          AND s.Deleted IS NULL;

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Sale not found.', 16, 1);
            RETURN;
        END

        UPDATE dbo.SalesItems
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        WHERE SalesId = @Id
          AND Deleted IS NULL;

        UPDATE dbo.SalsePaymentDetails
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        WHERE SalseId = @Id
          AND Deleted IS NULL;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

IF OBJECT_ID(N'dbo.usp_Sales_GetAll', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Sales_GetAll AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Sales_GetAll
    @FilterMode NVARCHAR(20) = NULL,
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.Id,
        s.CustomerId,
        s.billingType,
        s.PaymentType,
        s.BillNo,
        s.BillDate,
        s.MobileNo,
        s.Address,
        s.Total,
        s.TotalGstAmt,
        s.TotalPayable,
        s.PharmacyDoctorId,
        s.DoctorMobileNumber,
        s.DoctorRegNumber,
        s.Totaldiscount,
        s.discountPercent,
        s.discountAmount,
        s.PaidAmount,
        s.ReturnAmount,
        s.OfferId,
        s.SalesOrderId,
        s.Balance,
        s.NetCollection,
        s.RoundOffAmount,
        s.TotalCessAmount,
        s.PaymentStatus,
        s.TenantId,
        s.Created,
        s.CreatedBy,
        s.LastModified,
        s.LastModifiedBy,
        s.Deleted,
        s.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo,
        c.GSTNo AS CustomerGSTNo,
        c.Address AS CustomerAddress,
        c.Email AS CustomerEmail,
        d.Name AS PharmacyDoctorName,
        d.PhoneNo AS PharmacyDoctorPhoneNo,
        d.RegistrationNo AS PharmacyDoctorRegistrationNo
    INTO #FilteredSales
    FROM dbo.Saless s
    LEFT JOIN dbo.Customers c
        ON c.Id = s.CustomerId
       AND c.Deleted IS NULL
    LEFT JOIN dbo.PharmacyDoctors d
        ON d.Id = s.PharmacyDoctorId
       AND d.Deleted IS NULL
    WHERE s.Deleted IS NULL
      AND
      (
            @FilterMode IS NULL
            OR @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND s.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND s.CreatedBy = @UserId)
      );

    SELECT *
    FROM #FilteredSales
    ORDER BY ISNULL(Created, '19000101') DESC, Id DESC;

    SELECT
        si.Id,
        si.SalesId,
        si.ItemMasterId,
        si.PurchaseItemId,
        si.Batch,
        si.Qty,
        si.Rate,
        si.Gst,
        si.Discount,
        si.Amount,
        si.Expirydate,
        si.Mrp,
        si.IsSoldInTablets,
        si.TenantId,
        si.StripRate,
        si.Cess,
        si.Created,
        si.CreatedBy,
        si.LastModified,
        si.LastModifiedBy,
        si.Deleted,
        si.DeletedBy,
        im.Name AS ItemName,
        im.Code AS ItemCode,
        im.Barcode AS ItemBarcode,
        im.Conversion AS ItemConversion,
        im.Packing AS ItemPacking,
        im.IsActive AS ItemIsActive,
        im.MaximumDiscount AS ItemMaximumDiscount,
        im.MinimumQty AS ItemMinimumQty,
        im.CompanyId AS ItemCompanyId,
        im.HsnId AS ItemHsnId,
        im.Narcotics AS ItemNarcotics,
        im.ScheduleH AS ItemScheduleH,
        im.ScheduleH1 AS ItemScheduleH1,
        co.Name AS ItemCompanyName,
        h.HsnCode AS ItemHsnCode,
        h.IGST AS ItemHsnIGST,
        h.Cess AS ItemHsnCess,
        h.CGST AS ItemHsnCGST,
        h.SGST AS ItemHsnSGST
    FROM dbo.SalesItems si
    INNER JOIN #FilteredSales s
        ON s.Id = si.SalesId
    LEFT JOIN dbo.ItemMasters im
        ON im.Id = si.ItemMasterId
       AND im.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE si.Deleted IS NULL
    ORDER BY si.SalesId, si.Id;

    SELECT
        pd.Id,
        pd.SalseId,
        pd.PaymentModeId,
        pd.Amount,
        pd.ReferenceNo,
        pd.Description,
        pd.CustomerId,
        pd.SalesOrderId,
        pd.[Date],
        pd.TenantId,
        pd.Created,
        pd.CreatedBy,
        pd.LastModified,
        pd.LastModifiedBy,
        pd.Deleted,
        pd.DeletedBy,
        mop.Name AS PaymentModeName,
        mop.Description AS PaymentModeDescription
    FROM dbo.SalsePaymentDetails pd
    INNER JOIN #FilteredSales s
        ON s.Id = pd.SalseId
    LEFT JOIN dbo.ModeOfPayments mop
        ON mop.Id = pd.PaymentModeId
       AND mop.Deleted IS NULL
    WHERE pd.Deleted IS NULL
    ORDER BY pd.SalseId, pd.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_Sales_GetById', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Sales_GetById AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Sales_GetById
    @Id INT,
    @FilterMode NVARCHAR(20) = NULL,
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.Id,
        s.CustomerId,
        s.billingType,
        s.PaymentType,
        s.BillNo,
        s.BillDate,
        s.MobileNo,
        s.Address,
        s.Total,
        s.TotalGstAmt,
        s.TotalPayable,
        s.PharmacyDoctorId,
        s.DoctorMobileNumber,
        s.DoctorRegNumber,
        s.Totaldiscount,
        s.discountPercent,
        s.discountAmount,
        s.PaidAmount,
        s.ReturnAmount,
        s.OfferId,
        s.SalesOrderId,
        s.Balance,
        s.NetCollection,
        s.RoundOffAmount,
        s.TotalCessAmount,
        s.PaymentStatus,
        s.TenantId,
        s.Created,
        s.CreatedBy,
        s.LastModified,
        s.LastModifiedBy,
        s.Deleted,
        s.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo,
        c.GSTNo AS CustomerGSTNo,
        c.Address AS CustomerAddress,
        c.Email AS CustomerEmail,
        d.Name AS PharmacyDoctorName,
        d.PhoneNo AS PharmacyDoctorPhoneNo,
        d.RegistrationNo AS PharmacyDoctorRegistrationNo
    INTO #FilteredSalesById
    FROM dbo.Saless s
    LEFT JOIN dbo.Customers c
        ON c.Id = s.CustomerId
       AND c.Deleted IS NULL
    LEFT JOIN dbo.PharmacyDoctors d
        ON d.Id = s.PharmacyDoctorId
       AND d.Deleted IS NULL
    WHERE s.Id = @Id
      AND s.Deleted IS NULL
      AND
      (
            @FilterMode IS NULL
            OR @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND s.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND s.CreatedBy = @UserId)
      );

    SELECT *
    FROM #FilteredSalesById;

    SELECT
        si.Id,
        si.SalesId,
        si.ItemMasterId,
        si.PurchaseItemId,
        si.Batch,
        si.Qty,
        si.Rate,
        si.Gst,
        si.Discount,
        si.Amount,
        si.Expirydate,
        si.Mrp,
        si.IsSoldInTablets,
        si.TenantId,
        si.StripRate,
        si.Cess,
        si.Created,
        si.CreatedBy,
        si.LastModified,
        si.LastModifiedBy,
        si.Deleted,
        si.DeletedBy,
        im.Name AS ItemName,
        im.Code AS ItemCode,
        im.Barcode AS ItemBarcode,
        im.Conversion AS ItemConversion,
        im.Packing AS ItemPacking,
        im.IsActive AS ItemIsActive,
        im.MaximumDiscount AS ItemMaximumDiscount,
        im.MinimumQty AS ItemMinimumQty,
        im.CompanyId AS ItemCompanyId,
        im.HsnId AS ItemHsnId,
        co.Name AS ItemCompanyName,
        h.HsnCode AS ItemHsnCode,
        h.IGST AS ItemHsnIGST,
        h.Cess AS ItemHsnCess,
        h.CGST AS ItemHsnCGST,
        h.SGST AS ItemHsnSGST
    FROM dbo.SalesItems si
    INNER JOIN #FilteredSalesById s
        ON s.Id = si.SalesId
    LEFT JOIN dbo.ItemMasters im
        ON im.Id = si.ItemMasterId
       AND im.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE si.Deleted IS NULL
    ORDER BY si.Id;

    SELECT
        pd.Id,
        pd.SalseId,
        pd.PaymentModeId,
        pd.Amount,
        pd.ReferenceNo,
        pd.Description,
        pd.CustomerId,
        pd.SalesOrderId,
        pd.[Date],
        pd.TenantId,
        pd.Created,
        pd.CreatedBy,
        pd.LastModified,
        pd.LastModifiedBy,
        pd.Deleted,
        pd.DeletedBy,
        mop.Name AS PaymentModeName,
        mop.Description AS PaymentModeDescription
    FROM dbo.SalsePaymentDetails pd
    INNER JOIN #FilteredSalesById s
        ON s.Id = pd.SalseId
    LEFT JOIN dbo.ModeOfPayments mop
        ON mop.Id = pd.PaymentModeId
       AND mop.Deleted IS NULL
    WHERE pd.Deleted IS NULL
    ORDER BY pd.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_Sales_GetByCustomerId', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Sales_GetByCustomerId AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Sales_GetByCustomerId
    @CustomerId INT,
    @FilterMode NVARCHAR(20) = NULL,
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.Id,
        s.CustomerId,
        s.billingType,
        s.PaymentType,
        s.BillNo,
        s.BillDate,
        s.MobileNo,
        s.Address,
        s.Total,
        s.TotalGstAmt,
        s.TotalPayable,
        s.PharmacyDoctorId,
        s.DoctorMobileNumber,
        s.DoctorRegNumber,
        s.Totaldiscount,
        s.discountPercent,
        s.discountAmount,
        s.PaidAmount,
        s.ReturnAmount,
        s.OfferId,
        s.SalesOrderId,
        s.Balance,
        s.NetCollection,
        s.RoundOffAmount,
        s.TotalCessAmount,
        s.PaymentStatus,
        s.TenantId,
        s.Created,
        s.CreatedBy,
        s.LastModified,
        s.LastModifiedBy,
        s.Deleted,
        s.DeletedBy,
        c.Name AS CustomerName,
        c.PhoneNo AS CustomerPhoneNo,
        c.GSTNo AS CustomerGSTNo,
        c.Address AS CustomerAddress,
        c.Email AS CustomerEmail,
        d.Name AS PharmacyDoctorName,
        d.PhoneNo AS PharmacyDoctorPhoneNo,
        d.RegistrationNo AS PharmacyDoctorRegistrationNo
    INTO #FilteredSalesByCustomer
    FROM dbo.Saless s
    LEFT JOIN dbo.Customers c
        ON c.Id = s.CustomerId
       AND c.Deleted IS NULL
    LEFT JOIN dbo.PharmacyDoctors d
        ON d.Id = s.PharmacyDoctorId
       AND d.Deleted IS NULL
    WHERE s.CustomerId = @CustomerId
      AND s.Deleted IS NULL
      AND
      (
            @FilterMode IS NULL
            OR @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND s.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND s.CreatedBy = @UserId)
      );

    SELECT *
    FROM #FilteredSalesByCustomer
    ORDER BY ISNULL(Created, '19000101') DESC, Id DESC;

    SELECT
        si.Id,
        si.SalesId,
        si.ItemMasterId,
        si.PurchaseItemId,
        si.Batch,
        si.Qty,
        si.Rate,
        si.Gst,
        si.Discount,
        si.Amount,
        si.Expirydate,
        si.Mrp,
        si.IsSoldInTablets,
        si.TenantId,
        si.StripRate,
        si.Cess,
        si.Created,
        si.CreatedBy,
        si.LastModified,
        si.LastModifiedBy,
        si.Deleted,
        si.DeletedBy,
        im.Name AS ItemName,
        im.Code AS ItemCode,
        im.Barcode AS ItemBarcode,
        im.Conversion AS ItemConversion,
        im.Packing AS ItemPacking,
        im.IsActive AS ItemIsActive,
        im.MaximumDiscount AS ItemMaximumDiscount,
        im.MinimumQty AS ItemMinimumQty,
        im.CompanyId AS ItemCompanyId,
        im.HsnId AS ItemHsnId,
        co.Name AS ItemCompanyName,
        h.HsnCode AS ItemHsnCode,
        h.IGST AS ItemHsnIGST,
        h.Cess AS ItemHsnCess,
        h.CGST AS ItemHsnCGST,
        h.SGST AS ItemHsnSGST
    FROM dbo.SalesItems si
    INNER JOIN #FilteredSalesByCustomer s
        ON s.Id = si.SalesId
    LEFT JOIN dbo.ItemMasters im
        ON im.Id = si.ItemMasterId
       AND im.Deleted IS NULL
    LEFT JOIN dbo.Companies co
        ON co.Id = im.CompanyId
       AND co.Deleted IS NULL
    LEFT JOIN dbo.Hsns h
        ON h.Id = im.HsnId
       AND h.Deleted IS NULL
    WHERE si.Deleted IS NULL
    ORDER BY si.SalesId, si.Id;

    SELECT
        pd.Id,
        pd.SalseId,
        pd.PaymentModeId,
        pd.Amount,
        pd.ReferenceNo,
        pd.Description,
        pd.CustomerId,
        pd.SalesOrderId,
        pd.[Date],
        pd.TenantId,
        pd.Created,
        pd.CreatedBy,
        pd.LastModified,
        pd.LastModifiedBy,
        pd.Deleted,
        pd.DeletedBy,
        mop.Name AS PaymentModeName,
        mop.Description AS PaymentModeDescription
    FROM dbo.SalsePaymentDetails pd
    INNER JOIN #FilteredSalesByCustomer s
        ON s.Id = pd.SalseId
    LEFT JOIN dbo.ModeOfPayments mop
        ON mop.Id = pd.PaymentModeId
       AND mop.Deleted IS NULL
    WHERE pd.Deleted IS NULL
    ORDER BY pd.SalseId, pd.Id;
END
GO
