-- MERGED FROM TL: Drop and recreate PurchaseItemTvp if column types are int
IF TYPE_ID(N'dbo.PurchaseItemTvp') IS NOT NULL
   AND EXISTS
   (
       SELECT 1
       FROM sys.table_types tt
       JOIN sys.columns c
           ON c.object_id = tt.type_table_object_id
       JOIN sys.types ty
           ON ty.user_type_id = c.user_type_id
       WHERE tt.name = 'PurchaseItemTvp'
         AND c.name IN ('Qty', 'FreeQty')
       GROUP BY tt.name
       HAVING SUM(CASE WHEN ty.name = 'decimal' AND c.precision = 18 AND c.scale = 2 THEN 1 ELSE 0 END) < 2
   )
BEGIN
    IF OBJECT_ID(N'dbo.usp_Purchase_Save', N'P') IS NOT NULL
    BEGIN
        DROP PROCEDURE dbo.usp_Purchase_Save;
    END

    DROP TYPE dbo.PurchaseItemTvp;
END
GO

IF TYPE_ID(N'dbo.PurchaseItemTvp') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.PurchaseItemTvp AS TABLE
        (
            Id INT NULL,
            ItemId INT NOT NULL,
            Batch NVARCHAR(100) NULL,
            ExpiryDate DATETIME2 NULL,
            Mrp DECIMAL(18,2) NOT NULL,
            Qty DECIMAL(18,2) NOT NULL, -- MERGED FROM TL
            FreeQty DECIMAL(18,2) NOT NULL, -- MERGED FROM TL
            Unit NVARCHAR(50) NULL,
            Rate DECIMAL(18,2) NOT NULL,
            HsnId INT NULL,
            Gst DECIMAL(18,2) NOT NULL,
            GstAmount DECIMAL(18,2) NOT NULL,
            Discount DECIMAL(18,2) NOT NULL,
            DiscountAmt DECIMAL(18,2) NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            TotalAmt DECIMAL(18,2) NOT NULL,
            BatchWiseCose DECIMAL(18,2) NOT NULL,
            salserateA DECIMAL(18,2) NOT NULL,
            salserateB DECIMAL(18,2) NOT NULL,
            Barcode NVARCHAR(100) NULL,
            CGst DECIMAL(18,2) NOT NULL,
            SGst DECIMAL(18,2) NOT NULL,
            CGstAmount DECIMAL(18,2) NOT NULL,
            SGstAmount DECIMAL(18,2) NOT NULL,
            Cess DECIMAL(18,2) NOT NULL,
            SourcePurchaseChallanId INT NULL
        );
    ');
END
GO
IF TYPE_ID(N'dbo.PurchasePaymentDetailTvp') IS NULL
BEGIN
    EXEC(N'
        CREATE TYPE dbo.PurchasePaymentDetailTvp AS TABLE
        (
            Id INT NULL,
            [Date] DATETIME2 NULL,
            PaymentModeId INT NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            ReferenceNo NVARCHAR(200) NULL,
            [Description] NVARCHAR(1000) NULL
        );
    ');
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_GetAll', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_GetAll AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_GetAll
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.SupplierId,
        s.FirstName AS SupplierName,
        p.BillNo,
        p.BillDate,
        p.PartyBillNo,
        p.PartyBillDate,
        p.Total,
        p.TotalGstAmt,
        p.Totaldiscount,
        p.TotalPayable,
        p.discountPercent,
        p.discountAmount,
        p.PaymentAmt,
        p.PaymentStatus,
        p.RoundOffAmount,
        p.billingType,
        p.PaymentType,
        p.PurchaseType,
        p.TotalCGstAmt,
        p.TotalSGstAmt,
        p.TotalCessAmt,
        p.PaidAmount,
        p.ReturnAmount,
        p.Balance,
        p.PurchaseOrderId,
        p.TenantId,
        p.Created,
        p.CreatedBy,
        p.LastModified,
        p.LastModifiedBy,
        p.Deleted,
        p.DeletedBy
    INTO #FilteredPurchases
    FROM dbo.Purchases p
    LEFT JOIN dbo.Suppliers s
        ON s.Id = p.SupplierId
        AND s.Deleted IS NULL
    WHERE p.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
          );

    SELECT *
    FROM #FilteredPurchases
    ORDER BY ISNULL(Created, '19000101') DESC, Id DESC;

    SELECT
        pi.Id,
        pi.PurchaseId,
        pi.ItemId,
        pi.Batch,
        pi.ExpiryDate,
        pi.Mrp,
        pi.Qty,
        pi.FreeQty,
        pi.Unit,
        pi.Rate,
        pi.HsnId,
        pi.Gst,
        pi.GstAmount,
        pi.Discount,
        pi.DiscountAmt,
        pi.Amount,
        pi.TotalAmt,
        pi.BatchWiseCose,
        pi.salserateA,
        pi.salserateB,
        pi.Barcode,
        pi.CGst,
        pi.SGst,
        pi.CGstAmount,
        pi.SGstAmount,
        pi.Cess,
        pi.SourcePurchaseChallanId,
        pi.TenantId,
        pi.Created,
        pi.CreatedBy,
        pi.LastModified,
        pi.LastModifiedBy,
        pi.Deleted,
        pi.DeletedBy,
        im.Name AS ItemName,
        im.Conversion AS ItemConversion,
        im.Packing AS ItemPacking,
        im.CompanyId AS ItemCompanyId
    FROM dbo.PurchaseItems pi
    INNER JOIN #FilteredPurchases p
        ON p.Id = pi.PurchaseId
    LEFT JOIN dbo.ItemMasters im
        ON im.Id = pi.ItemId
        AND im.Deleted IS NULL
    WHERE pi.Deleted IS NULL
    ORDER BY pi.PurchaseId, pi.Id;

    SELECT
        pd.Id,
        pd.PurchaseId,
        pd.PaymentModeId,
        pd.Amount,
        pd.ReferenceNo,
        pd.Description,
        pd.[Date],
        pd.TenantId,
        pd.Created,
        pd.CreatedBy,
        pd.LastModified,
        pd.LastModifiedBy,
        pd.Deleted,
        pd.DeletedBy,
        mop.Name AS ModeOfPaymentName,
        mop.Description AS ModeOfPaymentDescription,
        mop.TenantId AS ModeOfPaymentTenantId,
        mop.PaymentType AS ModeOfPaymentType,
        mop.Created AS ModeOfPaymentCreated,
        mop.CreatedBy AS ModeOfPaymentCreatedBy,
        mop.LastModified AS ModeOfPaymentLastModified,
        mop.LastModifiedBy AS ModeOfPaymentLastModifiedBy,
        mop.Deleted AS ModeOfPaymentDeleted,
        mop.DeletedBy AS ModeOfPaymentDeletedBy
    FROM dbo.SalsePaymentDetails pd
    INNER JOIN #FilteredPurchases p
        ON p.Id = pd.PurchaseId
    LEFT JOIN dbo.ModeOfPayments mop
        ON mop.Id = pd.PaymentModeId
        AND mop.Deleted IS NULL
    WHERE pd.Deleted IS NULL
    ORDER BY pd.PurchaseId, pd.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_GetById', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_GetById AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_GetById
    @Id INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.SupplierId,
        s.FirstName AS SupplierName,
        p.BillNo,
        p.BillDate,
        p.PartyBillNo,
        p.PartyBillDate,
        p.Total,
        p.TotalGstAmt,
        p.Totaldiscount,
        p.TotalPayable,
        p.discountPercent,
        p.discountAmount,
        p.PaymentAmt,
        p.PaymentStatus,
        p.RoundOffAmount,
        p.billingType,
        p.PaymentType,
        p.PurchaseType,
        p.TotalCGstAmt,
        p.TotalSGstAmt,
        p.TotalCessAmt,
        p.PaidAmount,
        p.ReturnAmount,
        p.Balance,
        p.PurchaseOrderId,
        p.TenantId,
        p.Created,
        p.CreatedBy,
        p.LastModified,
        p.LastModifiedBy,
        p.Deleted,
        p.DeletedBy
    INTO #FilteredPurchase
    FROM dbo.Purchases p
    LEFT JOIN dbo.Suppliers s
        ON s.Id = p.SupplierId
        AND s.Deleted IS NULL
    WHERE p.Id = @Id
      AND p.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
          );

    SELECT *
    FROM #FilteredPurchase;

    SELECT
        pi.Id,
        pi.PurchaseId,
        pi.ItemId,
        pi.Batch,
        pi.ExpiryDate,
        pi.Mrp,
        pi.Qty,
        pi.FreeQty,
        pi.Unit,
        pi.Rate,
        pi.HsnId,
        pi.Gst,
        pi.GstAmount,
        pi.Discount,
        pi.DiscountAmt,
        pi.Amount,
        pi.TotalAmt,
        pi.BatchWiseCose,
        pi.salserateA,
        pi.salserateB,
        pi.Barcode,
        pi.CGst,
        pi.SGst,
        pi.CGstAmount,
        pi.SGstAmount,
        pi.Cess,
        pi.SourcePurchaseChallanId,
        pi.TenantId,
        pi.Created,
        pi.CreatedBy,
        pi.LastModified,
        pi.LastModifiedBy,
        pi.Deleted,
        pi.DeletedBy,
        im.Name AS ItemName,
        im.Conversion AS ItemConversion,
        im.Packing AS ItemPacking,
        im.CompanyId AS ItemCompanyId
    FROM dbo.PurchaseItems pi
    INNER JOIN #FilteredPurchase p
        ON p.Id = pi.PurchaseId
    LEFT JOIN dbo.ItemMasters im
        ON im.Id = pi.ItemId
        AND im.Deleted IS NULL
    WHERE pi.Deleted IS NULL
    ORDER BY pi.Id;

    SELECT
        pd.Id,
        pd.PurchaseId,
        pd.PaymentModeId,
        pd.Amount,
        pd.ReferenceNo,
        pd.Description,
        pd.[Date],
        pd.TenantId,
        pd.Created,
        pd.CreatedBy,
        pd.LastModified,
        pd.LastModifiedBy,
        pd.Deleted,
        pd.DeletedBy,
        mop.Name AS ModeOfPaymentName,
        mop.Description AS ModeOfPaymentDescription,
        mop.TenantId AS ModeOfPaymentTenantId,
        mop.PaymentType AS ModeOfPaymentType,
        mop.Created AS ModeOfPaymentCreated,
        mop.CreatedBy AS ModeOfPaymentCreatedBy,
        mop.LastModified AS ModeOfPaymentLastModified,
        mop.LastModifiedBy AS ModeOfPaymentLastModifiedBy,
        mop.Deleted AS ModeOfPaymentDeleted,
        mop.DeletedBy AS ModeOfPaymentDeletedBy
    FROM dbo.SalsePaymentDetails pd
    INNER JOIN #FilteredPurchase p
        ON p.Id = pd.PurchaseId
    LEFT JOIN dbo.ModeOfPayments mop
        ON mop.Id = pd.PaymentModeId
        AND mop.Deleted IS NULL
    WHERE pd.Deleted IS NULL
    ORDER BY pd.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_GetBySupplierId', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_GetBySupplierId AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_GetBySupplierId
    @SupplierId INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id,
        p.SupplierId,
        s.FirstName AS SupplierName,
        p.BillNo,
        p.BillDate,
        p.PartyBillNo,
        p.PartyBillDate,
        p.Total,
        p.TotalGstAmt,
        p.Totaldiscount,
        p.TotalPayable,
        p.discountPercent,
        p.discountAmount,
        p.PaymentAmt,
        p.PaymentStatus,
        p.RoundOffAmount,
        p.billingType,
        p.PaymentType,
        p.PurchaseType,
        p.TotalCGstAmt,
        p.TotalSGstAmt,
        p.TotalCessAmt,
        p.PaidAmount,
        p.ReturnAmount,
        p.Balance,
        p.PurchaseOrderId,
        p.TenantId,
        p.Created,
        p.CreatedBy,
        p.LastModified,
        p.LastModifiedBy,
        p.Deleted,
        p.DeletedBy
    FROM dbo.Purchases p
    LEFT JOIN dbo.Suppliers s
        ON s.Id = p.SupplierId
        AND s.Deleted IS NULL
    WHERE p.SupplierId = @SupplierId
      AND p.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
          )
    ORDER BY p.BillDate DESC, p.Id DESC;
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_Save', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_Save AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_Save
    @Id INT = NULL,
    @SupplierId INT = NULL,
    @BillNo NVARCHAR(MAX),
    @BillDate DATETIME2 = NULL,
    @PartyBillNo NVARCHAR(MAX) = NULL,
    @PartyBillDate DATETIME2 = NULL,
    @Total DECIMAL(18,2),
    @TotalGstAmt DECIMAL(18,2),
    @Totaldiscount DECIMAL(18,2),
    @TotalPayable DECIMAL(18,2),
    @discountPercent DECIMAL(18,2),
    @discountAmount DECIMAL(18,2),
    @PaymentAmt DECIMAL(18,2),
    @PaymentStatus NVARCHAR(MAX) = NULL,
    @RoundOffAmount DECIMAL(18,2),
    @billingType NVARCHAR(MAX) = NULL,
    @PaymentType NVARCHAR(MAX) = NULL,
    @PurchaseType NVARCHAR(MAX) = NULL,
    @TotalCGstAmt DECIMAL(18,2),
    @TotalSGstAmt DECIMAL(18,2),
    @TotalCessAmt DECIMAL(18,2),
    @PaidAmount DECIMAL(18,2),
    @ReturnAmount DECIMAL(18,2),
    @Balance DECIMAL(18,2),
    @PurchaseOrderId INT = NULL,
    @PurchaseItems dbo.PurchaseItemTvp READONLY,
    @PaymentDetails dbo.PurchasePaymentDetailTvp READONLY,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PurchaseId INT = NULLIF(@Id, 0);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @PurchaseId IS NULL
        BEGIN
            INSERT INTO dbo.Purchases
            (
                SupplierId,
                BillNo,
                BillDate,
                PartyBillNo,
                PartyBillDate,
                Total,
                TotalGstAmt,
                Totaldiscount,
                TotalPayable,
                discountPercent,
                discountAmount,
                PaymentAmt,
                PaymentStatus,
                RoundOffAmount,
                billingType,
                PaymentType,
                PurchaseType,
                TotalCGstAmt,
                TotalSGstAmt,
                TotalCessAmt,
                PaidAmount,
                ReturnAmount,
                Balance,
                PurchaseOrderId,
                TenantId,
                Created,
                CreatedBy
            )
            VALUES
            (
                @SupplierId,
                @BillNo,
                @BillDate,
                ISNULL(@PartyBillNo, N''),
                @PartyBillDate,
                @Total,
                @TotalGstAmt,
                @Totaldiscount,
                @TotalPayable,
                @discountPercent,
                @discountAmount,
                @PaymentAmt,
                @PaymentStatus,
                @RoundOffAmount,
                @billingType,
                @PaymentType,
                @PurchaseType,
                @TotalCGstAmt,
                @TotalSGstAmt,
                @TotalCessAmt,
                @PaidAmount,
                @ReturnAmount,
                @Balance,
                @PurchaseOrderId,
                @TenantId,
                @Now,
                @UserId
            );

            SET @PurchaseId = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE p
            SET
                SupplierId = @SupplierId,
                BillNo = @BillNo,
                BillDate = @BillDate,
                PartyBillNo = ISNULL(@PartyBillNo, N''),
                PartyBillDate = @PartyBillDate,
                Total = @Total,
                TotalGstAmt = @TotalGstAmt,
                Totaldiscount = @Totaldiscount,
                TotalPayable = @TotalPayable,
                discountPercent = @discountPercent,
                discountAmount = @discountAmount,
                PaymentAmt = @PaymentAmt,
                PaymentStatus = @PaymentStatus,
                RoundOffAmount = @RoundOffAmount,
                billingType = @billingType,
                PaymentType = @PaymentType,
                PurchaseType = @PurchaseType,
                TotalCGstAmt = @TotalCGstAmt,
                TotalSGstAmt = @TotalSGstAmt,
                TotalCessAmt = @TotalCessAmt,
                PaidAmount = @PaidAmount,
                ReturnAmount = @ReturnAmount,
                Balance = @Balance,
                PurchaseOrderId = @PurchaseOrderId,
                TenantId = COALESCE(p.TenantId, @TenantId),
                LastModified = @Now,
                LastModifiedBy = @UserId
            FROM dbo.Purchases p
            WHERE p.Id = @PurchaseId
              AND p.Deleted IS NULL
              AND (
                    @FilterMode = N'All'
                    OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
                    OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
                  );

            IF @@ROWCOUNT = 0
            BEGIN
                RAISERROR('Purchase not found or access denied.', 16, 1);
                RETURN;
            END
        END

        UPDATE pi
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.PurchaseItems pi
        WHERE pi.PurchaseId = @PurchaseId
          AND pi.Deleted IS NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM @PurchaseItems src
              WHERE ISNULL(src.Id, 0) > 0
                AND src.Id = pi.Id
          );

        UPDATE pi
        SET
            ItemId = src.ItemId,
            Batch = src.Batch,
            ExpiryDate = src.ExpiryDate,
            Mrp = src.Mrp,
            Qty = src.Qty,
            FreeQty = src.FreeQty,
            Unit = src.Unit,
            Rate = src.Rate,
            HsnId = src.HsnId,
            Gst = src.Gst,
            GstAmount = src.GstAmount,
            Discount = src.Discount,
            DiscountAmt = src.DiscountAmt,
            Amount = src.Amount,
            TotalAmt = src.TotalAmt,
            BatchWiseCose = src.BatchWiseCose,
            salserateA = src.salserateA,
            salserateB = src.salserateB,
            Barcode = src.Barcode,
            CGst = src.CGst,
            SGst = src.SGst,
            CGstAmount = src.CGstAmount,
            SGstAmount = src.SGstAmount,
            Cess = src.Cess,
            SourcePurchaseChallanId = src.SourcePurchaseChallanId,
            TenantId = COALESCE(pi.TenantId, @TenantId),
            LastModified = @Now,
            LastModifiedBy = @UserId
        FROM dbo.PurchaseItems pi
        INNER JOIN @PurchaseItems src
            ON src.Id = pi.Id
        WHERE pi.PurchaseId = @PurchaseId
          AND pi.Deleted IS NULL;

        INSERT INTO dbo.PurchaseItems
        (
            PurchaseId,
            Batch,
            ItemId,
            Qty,
            FreeQty,
            Unit,
            Rate,
            HsnId,
            Gst,
            GstAmount,
            Discount,
            DiscountAmt,
            ExpiryDate,
            Amount,
            TotalAmt,
            TenantId,
            BatchWiseCose,
            Mrp,
            salserateA,
            salserateB,
            Barcode,
            CGst,
            SGst,
            CGstAmount,
            SGstAmount,
            SourcePurchaseChallanId,
            Cess,
            Created,
            CreatedBy
        )
        SELECT
            @PurchaseId,
            src.Batch,
            src.ItemId,
            src.Qty,
            src.FreeQty,
            src.Unit,
            src.Rate,
            src.HsnId,
            src.Gst,
            src.GstAmount,
            src.Discount,
            src.DiscountAmt,
            src.ExpiryDate,
            src.Amount,
            src.TotalAmt,
            @TenantId,
            src.BatchWiseCose,
            src.Mrp,
            src.salserateA,
            src.salserateB,
            src.Barcode,
            src.CGst,
            src.SGst,
            src.CGstAmount,
            src.SGstAmount,
            src.SourcePurchaseChallanId,
            src.Cess,
            @Now,
            @UserId
        FROM @PurchaseItems src
        WHERE ISNULL(src.Id, 0) = 0;

        UPDATE pd
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.SalsePaymentDetails pd
        WHERE pd.PurchaseId = @PurchaseId
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
            TenantId = COALESCE(pd.TenantId, @TenantId),
            LastModified = @Now,
            LastModifiedBy = @UserId
        FROM dbo.SalsePaymentDetails pd
        INNER JOIN @PaymentDetails src
            ON src.Id = pd.Id
        WHERE pd.PurchaseId = @PurchaseId
          AND pd.Deleted IS NULL;

        INSERT INTO dbo.SalsePaymentDetails
        (
            PaymentModeId,
            Amount,
            ReferenceNo,
            [Description],
            TenantId,
            PurchaseId,
            [Date],
            Created,
            CreatedBy
        )
        SELECT
            src.PaymentModeId,
            src.Amount,
            src.ReferenceNo,
            src.[Description],
            @TenantId,
            @PurchaseId,
            src.[Date],
            @Now,
            @UserId
        FROM @PaymentDetails src
        WHERE ISNULL(src.Id, 0) = 0;

        COMMIT TRANSACTION;

        SELECT @PurchaseId AS PurchaseId;
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

IF OBJECT_ID(N'dbo.usp_Purchase_Delete', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_Delete AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_Delete
    @Id INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE p
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        FROM dbo.Purchases p
        WHERE p.Id = @Id
          AND p.Deleted IS NULL
          AND (
                @FilterMode = N'All'
                OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
                OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
              );

        IF @@ROWCOUNT = 0
        BEGIN
            RAISERROR('Purchase not found or access denied.', 16, 1);
            RETURN;
        END

        UPDATE dbo.PurchaseItems
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        WHERE PurchaseId = @Id
          AND Deleted IS NULL;

        UPDATE dbo.SalsePaymentDetails
        SET
            Deleted = @Now,
            DeletedBy = @UserId
        WHERE PurchaseId = @Id
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

IF OBJECT_ID(N'dbo.usp_Purchase_GetItemSupplierPurchaseInfo', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_GetItemSupplierPurchaseInfo AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_GetItemSupplierPurchaseInfo
    @ItemId INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (5)
        s.FirstName AS Supplier,
        p.BillNo,
        p.BillDate,
        pi.Qty,
        pi.FreeQty,
        pi.Batch,
        pi.Mrp,
        pi.Rate,
        pi.ExpiryDate,
        p.Totaldiscount AS TotalDiscount,
        pi.BatchWiseCose AS BatchWiseCost
    FROM dbo.PurchaseItems pi
    INNER JOIN dbo.Purchases p
        ON p.Id = pi.PurchaseId
    LEFT JOIN dbo.Suppliers s
        ON s.Id = p.SupplierId
        AND s.Deleted IS NULL
    WHERE pi.ItemId = @ItemId
      AND pi.Deleted IS NULL
      AND p.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
          )
    ORDER BY ISNULL(p.Created, '19000101') DESC, p.Id DESC, pi.Id DESC;
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_GetItemHistory', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_GetItemHistory AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_GetItemHistory
    @ItemId INT,
    @SupplierId INT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (5)
        s.FirstName AS Supplier,
        p.BillNo,
        p.BillDate,
        pi.Qty,
        pi.FreeQty,
        pi.Batch,
        pi.Mrp,
        pi.Rate,
        pi.ExpiryDate,
        p.Totaldiscount AS TotalDiscount,
        pi.BatchWiseCose AS BatchWiseCost
    FROM dbo.PurchaseItems pi
    INNER JOIN dbo.Purchases p
        ON p.Id = pi.PurchaseId
    LEFT JOIN dbo.Suppliers s
        ON s.Id = p.SupplierId
        AND s.Deleted IS NULL
    WHERE pi.ItemId = @ItemId
      AND p.SupplierId = @SupplierId
      AND pi.Deleted IS NULL
      AND p.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND p.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND p.CreatedBy = @UserId)
          )
    ORDER BY ISNULL(p.Created, '19000101') DESC, p.Id DESC, pi.Id DESC;
END
GO

IF OBJECT_ID(N'dbo.usp_CurrentStock_GetAll', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_CurrentStock_GetAll AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_CurrentStock_GetAll
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        cs.Id,
        cs.ItemId,
        cs.Batch,
        cs.ExpiryDate,
        cs.Qty,
        cs.PurchaseRate,
        cs.SalesRateA,
        cs.SalesRateB,
        cs.Barcode,
        cs.Mrp,
        cs.TenantId,
        cs.Created,
        cs.CreatedBy,
        cs.LastModified,
        cs.LastModifiedBy,
        cs.Deleted,
        cs.DeletedBy
    FROM dbo.CurrentStocks cs
    WHERE cs.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND cs.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND cs.CreatedBy = @UserId)
          )
    ORDER BY cs.ItemId, cs.Batch, cs.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_CurrentStock_Get', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_CurrentStock_Get AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_CurrentStock_Get
    @ItemId INT,
    @Batch NVARCHAR(100) = NULL,
    @ExpiryDate DATETIME2 = NULL,
    @Mrp DECIMAL(18,2),
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        cs.Id,
        cs.ItemId,
        cs.Batch,
        cs.ExpiryDate,
        cs.Qty,
        cs.PurchaseRate,
        cs.SalesRateA,
        cs.SalesRateB,
        cs.Barcode,
        cs.Mrp,
        cs.TenantId,
        cs.Created,
        cs.CreatedBy,
        cs.LastModified,
        cs.LastModifiedBy,
        cs.Deleted,
        cs.DeletedBy
    FROM dbo.CurrentStocks cs
    WHERE cs.Deleted IS NULL
      AND cs.ItemId = @ItemId
      AND ISNULL(cs.Batch, N'') = ISNULL(@Batch, N'')
      AND cs.Mrp = @Mrp
      AND (
            (cs.ExpiryDate IS NULL AND @ExpiryDate IS NULL)
            OR cs.ExpiryDate = @ExpiryDate
          )
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND cs.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND cs.CreatedBy = @UserId)
          )
    ORDER BY cs.Id;
END
GO

IF OBJECT_ID(N'dbo.usp_CurrentStock_ApplyDelta', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_CurrentStock_ApplyDelta AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_CurrentStock_ApplyDelta
    @ItemId INT,
    @Batch NVARCHAR(100) = NULL,
    @QtyChange DECIMAL(18,2),
    @ExpiryDate DATETIME2 = NULL,
    @Mrp DECIMAL(18,2),
    @SalesRateA DECIMAL(18,2),
    @SalesRateB DECIMAL(18,2) = NULL,
    @PurchaseRate DECIMAL(18,2) = NULL,
    @Barcode NVARCHAR(100) = NULL,
    @ForceUpdate BIT,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StockId INT;

    SELECT TOP (1)
        @StockId = cs.Id
    FROM dbo.CurrentStocks cs
    WHERE cs.Deleted IS NULL
      AND cs.ItemId = @ItemId
      AND ISNULL(cs.Batch, N'') = ISNULL(@Batch, N'')
      AND cs.Mrp = @Mrp
      AND (
            (cs.ExpiryDate IS NULL AND @ExpiryDate IS NULL)
            OR cs.ExpiryDate = @ExpiryDate
          )
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND cs.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND cs.CreatedBy = @UserId)
          )
    ORDER BY cs.Id;

    IF @StockId IS NOT NULL
    BEGIN
        UPDATE dbo.CurrentStocks
        SET
            Qty = ROUND(Qty + @QtyChange, 2),
            SalesRateA = @SalesRateA,
            SalesRateB = ISNULL(@SalesRateB, SalesRateB),
            PurchaseRate = ISNULL(@PurchaseRate, PurchaseRate),
            Barcode = COALESCE(NULLIF(@Barcode, ''), Barcode),
            LastModified = @Now,
            LastModifiedBy = @UserId
        WHERE Id = @StockId;

        RETURN;
    END

    INSERT INTO dbo.CurrentStocks
    (
        ItemId,
        Batch,
        ExpiryDate,
        Qty,
        PurchaseRate,
        SalesRateA,
        SalesRateB,
        Barcode,
        Mrp,
        TenantId,
        Created,
        CreatedBy
    )
    VALUES
    (
        @ItemId,
        @Batch,
        @ExpiryDate,
        ROUND(@QtyChange, 2),
        ISNULL(@PurchaseRate, 0),
        @SalesRateA,
        ISNULL(@SalesRateB, 0),
        @Barcode,
        @Mrp,
        @TenantId,
        @Now,
        @UserId
    );
END
GO

IF OBJECT_ID(N'dbo.usp_CurrentStock_Overwrite', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_CurrentStock_Overwrite AS BEGIN SET NOCOUNT ON; END');
END
GO

-- MERGED FROM TL: Updated usp_CurrentStock_Overwrite parameters
ALTER PROCEDURE dbo.usp_CurrentStock_Overwrite
    @Id INT,
    @Batch NVARCHAR(100) = NULL,
    @ExpiryDate DATETIME2 = NULL,
    @Mrp DECIMAL(18,2),
    @Qty DECIMAL(18,2),
    @PurchaseRate DECIMAL(18,2),
    @SalesRateA DECIMAL(18,2) = NULL,
    @SalesRateB DECIMAL(18,2) = NULL,
    @Barcode NVARCHAR(100) = NULL,
    @FilterMode NVARCHAR(20),
    @TenantId NVARCHAR(450) = NULL,
    @UserId NVARCHAR(450),
    @Now DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE cs
    SET
        Batch = @Batch,
        ExpiryDate = @ExpiryDate,
        Mrp = @Mrp,
        Qty = ROUND(@Qty, 2),
        PurchaseRate = @PurchaseRate,
        SalesRateA = ISNULL(@SalesRateA, SalesRateA),
        SalesRateB = ISNULL(@SalesRateB, SalesRateB),
        Barcode = COALESCE(NULLIF(@Barcode, ''), Barcode),
        TenantId = COALESCE(cs.TenantId, @TenantId),
        LastModified = @Now,
        LastModifiedBy = @UserId
    FROM dbo.CurrentStocks cs
    WHERE cs.Id = @Id
      AND cs.Deleted IS NULL
      AND (
            @FilterMode = N'All'
            OR (@FilterMode = N'Tenant' AND cs.TenantId = @TenantId)
            OR (@FilterMode = N'User' AND cs.CreatedBy = @UserId)
          );

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Current stock not found or access denied.', 16, 1);
        RETURN;
    END
END
GO

IF OBJECT_ID(N'dbo.usp_Purchase_IsBillNoDuplicate', N'P') IS NULL
BEGIN
    EXEC(N'CREATE PROCEDURE dbo.usp_Purchase_IsBillNoDuplicate AS BEGIN SET NOCOUNT ON; END');
END
GO

ALTER PROCEDURE dbo.usp_Purchase_IsBillNoDuplicate
(
    @BillNo NVARCHAR(100),
    @Id INT = 0,              -- Edit case
    @TenantId NVARCHAR(50) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CASE 
            WHEN EXISTS (
                SELECT 1 
                FROM dbo.Purchases 
                WHERE BillNo = @BillNo
                  AND (@Id = 0 OR Id != @Id)   -- Edit ignore
                  AND (@TenantId IS NULL OR TenantId = @TenantId)
                  AND Deleted IS NULL
            )
            THEN CAST(1 AS BIT)
            ELSE CAST(0 AS BIT)
        END AS IsDuplicate;
END
