using AOne.Models.Entity;
using ClosedXML.Excel;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using NPOI.SS.UserModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace EasyBill.UI.Service.PurchaseImportService
{
    public static class PurchaseImportBuilder
    {
        private static readonly string[] ImportItemNameAliases = { "Item", "Item Name", "Medicine", "Medicine Name", "Product", "Product Name", "Description", "Particular", "Particulars", "Name" };
        private static readonly string[] ImportItemCodeAliases = { "Code", "Item Code", "Product Code", "Medicine Code" };
        private static readonly string[] ImportBarcodeAliases = { "Barcode", "Bar Code" };
        private static readonly string[] ImportBatchAliases = { "Batch", "Batch No", "Batch No.", "Batch Number", "BatchNo" };
        private static readonly string[] ImportExpiryAliases = { "Expiry", "Expiry Date", "ExpiryDate", "Exp Date", "Exp", "Exp.", "Expiry Month", "Use Before" };
        private static readonly string[] ImportQtyAliases = { "Qty", "Qty.", "Quantity" };
        private static readonly string[] ImportPackAliases = { "Pack", "Packing", "Size", "Pack Size", "Mfr", "MFG", "Manufacturer" };
        private static readonly string[] ImportFreeQtyAliases = { "Free", "Free Qty", "Free Quantity", "Bonus Qty", "Scheme Qty" };
        private static readonly string[] ImportRateAliases = { "Rate", "Purchase Rate", "PTR", "Net Rate", "Cost Rate" };
        private static readonly string[] ImportMrpAliases = { "MRP", "M.R.P", "Mrp" };
        private static readonly string[] ImportDiscountAliases = { "Discount", "Discount %", "Disc", "Disc %", "DIS", "DISC" };
        private static readonly string[] ImportGstAliases = { "GST", "GST %", "IGST", "IGST %", "Tax", "Tax %" };
        private static readonly string[] ImportCgstAliases = { "CGST", "CGST %" };
        private static readonly string[] ImportSgstAliases = { "SGST", "SGST %" };
        private static readonly string[] ImportCessAliases = { "Cess", "Cess %" };
        private static readonly string[] ImportCessAmountAliases = { "Cess Amount", "Cess Amt" };
        private static readonly string[] ImportAmountAliases = { "Amount", "Line Amount", "Line Total", "Net Amount", "Net Amt", "Taxable Amount", "Gross Amount" };
        private static readonly string[] ImportSalesRateAAliases = { "Sales Rate A", "Sale Rate A", "Rate A", "SalesRateA" };
        private static readonly string[] ImportSalesRateBAliases = { "Sales Rate B", "Sale Rate B", "Rate B", "SalesRateB" };
        private static readonly string[] ImportBillNoAliases = { "Bill No", "Bill Number", "Invoice No", "Invoice Number", "Invoice #" };
        private static readonly string[] ImportBillDateAliases = { "Bill Date", "Invoice Date", "Date" };
        private static readonly string[] ImportPartyBillNoAliases = { "Party Bill No", "Supplier Bill No", "Vendor Bill No" };
        private static readonly string[] ImportPartyBillDateAliases = { "Party Bill Date", "Supplier Bill Date", "Vendor Bill Date" };
        private static readonly string[] ImportSupplierAliases = { "Supplier", "Supplier Name", "Vendor", "Vendor Name", "Party Name" };
        private static readonly string[] ImportPurchaseTypeAliases = { "Purchase Type", "Type" };

        private static readonly HashSet<string> NonItemLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "GST", "SGST", "CGST", "IGST", "CESS",
            "TOTAL", "SUBTOTAL", "SUB TOTAL", "GRAND TOTAL", "NET TOTAL", "NET AMOUNT",
            "CLASS TOTAL", "TAXABLE VALUE", "ROUNDOFF", "ROUND OFF", "DISCOUNT"
        };

        private static readonly string[] SummaryRowContainsKeywords =
        {
            "CLASSTOTAL", "GRANDTOTAL", "SUBTOTAL", "NETTOTAL", "NETAMOUNT", "TOTALITEM", "TOTALQTY", "TOTALPCS",
            "TAXABLEVALUE", "AMOUNTINWORDS", "TERMSCONDITIONS", "TERMS", "BANKDETAIL", "ACCOUNTDETAIL",
            "AUTHORIZEDSIGNATORY", "SGSTPAYABLE", "CGSTPAYABLE", "IGSTPAYABLE", "GSTPAYABLE", "ROUNDOFF"
        };

        private const double PdfLineTolerance = 1.5d;
        private const double PdfContinuationTolerance = 12d;
        private const double PdfColumnSnapTolerance = 8d;

        private sealed class PurchaseImportRawRow
        {
            public int RowNumber { get; set; }
            public string? ItemName { get; set; }
            public string? ItemCode { get; set; }
            public string? Barcode { get; set; }
            public string? PackText { get; set; }
            public string? Batch { get; set; }
            public string? ExpiryText { get; set; }
            public string? QtyText { get; set; }
            public string? FreeQtyText { get; set; }
            public string? RateText { get; set; }
            public string? MrpText { get; set; }
            public string? DiscountText { get; set; }
            public string? GstText { get; set; }
            public string? CGstText { get; set; }
            public string? SGstText { get; set; }
            public string? CessText { get; set; }
            public string? CessAmountText { get; set; }
            public string? AmountText { get; set; }
            public string? SalesRateAText { get; set; }
            public string? SalesRateBText { get; set; }
            public string RawText { get; set; } = string.Empty;
        }

        private sealed class PdfImportReadResult
        {
            public List<PurchaseImportRawRow> Rows { get; } = new();
            public List<string> Lines { get; } = new();
            public string FullText => string.Join(Environment.NewLine, Lines);
        }

        private sealed record PdfWordCell(string Text, double X, double Y);

        private sealed class PdfLineRow
        {
            public PdfLineRow(int pageNumber, double y)
            {
                PageNumber = pageNumber;
                Y = y;
            }

            public int PageNumber { get; }
            public double Y { get; }
            public List<PdfWordCell> Words { get; } = new();
            public string Text { get; set; } = string.Empty;
        }

        private sealed record PdfColumnBoundary(string Key, double StartX, double EndX);

        public static async Task<PurchaseImportPreviewVM> BuildAsync(
            IFormFile file,
            IEnumerable<ItemMaster> activeItems,
            IEnumerable<Supplier> suppliers)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            return extension switch
            {
                ".pdf" => await BuildPdfImportPreviewAsync(file, activeItems, suppliers),
                ".xlsx" or ".xls" or ".csv" => await BuildStructuredImportPreviewAsync(file, activeItems, suppliers),
                _ => throw new InvalidOperationException("Supported import files are .xlsx, .xls, .csv, and text-based .pdf.")
            };
        }

        public static byte[] BuildTemplateFile()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("PurchaseImport");

            sheet.Cell(1, 1).Value = "Bill No";
            sheet.Cell(1, 2).Value = "PB-1024";
            sheet.Cell(2, 1).Value = "Bill Date";
            sheet.Cell(2, 2).Value = DateTime.Today.ToString("dd/MM/yyyy");
            sheet.Cell(3, 1).Value = "Party Bill No";
            sheet.Cell(3, 2).Value = "VENDOR-778";
            sheet.Cell(4, 1).Value = "Party Bill Date";
            sheet.Cell(4, 2).Value = DateTime.Today.ToString("dd/MM/yyyy");
            sheet.Cell(5, 1).Value = "Supplier Name";
            sheet.Cell(5, 2).Value = "ABC Pharma";
            sheet.Cell(6, 1).Value = "Purchase Type";
            sheet.Cell(6, 2).Value = "Local";

            var headers = new[]
            {
                "Item Code", "Item Name", "Barcode", "Batch", "Expiry Date",
                "Qty", "Free Qty", "Rate", "MRP", "Discount", "GST %", "Cess %",
                "Sales Rate A", "Sales Rate B"
            };

            for (var column = 0; column < headers.Length; column++)
            {
                sheet.Cell(8, column + 1).Value = headers[column];
                sheet.Cell(8, column + 1).Style.Font.Bold = true;
                sheet.Cell(8, column + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            sheet.Cell(9, 1).Value = "ITEM-001";
            sheet.Cell(9, 2).Value = "Paracetamol 650";
            sheet.Cell(9, 3).Value = "8901234567890";
            sheet.Cell(9, 4).Value = "BATCH-001";
            sheet.Cell(9, 5).Value = "05/2027";
            sheet.Cell(9, 6).Value = 10;
            sheet.Cell(9, 7).Value = 1;
            sheet.Cell(9, 8).Value = 54.25;
            sheet.Cell(9, 9).Value = 72.00;
            sheet.Cell(9, 10).Value = 0;
            sheet.Cell(9, 11).Value = 12;
            sheet.Cell(9, 12).Value = 0;
            sheet.Cell(9, 13).Value = 68.00;
            sheet.Cell(9, 14).Value = 70.00;

            sheet.Cell(11, 1).Value = "Notes";
            sheet.Cell(11, 2).Value = "Item can be matched by Barcode, Item Code, exact or close Item Name. Batch, Qty, and Rate can also be completed manually after import.";
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Preview builders
        private static async Task<PurchaseImportPreviewVM> BuildStructuredImportPreviewAsync(
            IFormFile file,
            IEnumerable<ItemMaster> activeItems,
            IEnumerable<Supplier> suppliers)
        {
            var matrix = await ReadImportMatrixAsync(file);
            if (!matrix.Any())
            {
                if (!matrix.Any())
                {
                    // 🟢 BTR UPDATE: User-friendly error for scanned PDFs
                    throw new InvalidOperationException("The selected PDF appears to be scanned or image-based. Text import currently works with selectable-text PDF, Excel, and CSV files.");
                }
            }

            var headerRowIndex = FindImportHeaderRowIndex(matrix);
            if (headerRowIndex < 0)
            {
                throw new InvalidOperationException(
                    "Item header row not found. Use the template or include columns like Item/Item Code, Batch, Qty, and Rate.");
            }

            var rowMaps = ConvertImportRowsToDictionaries(matrix, headerRowIndex);
            var preview = new PurchaseImportPreviewVM
            {
                DetectedFormat = GetDetectedImportFormat(file.FileName),
                Header = BuildImportHeader(matrix, headerRowIndex, rowMaps, suppliers)
            };

            var rawRows = rowMaps
                .Select((rowMap, index) => MapPurchaseImportRawRow(rowMap, headerRowIndex + index + 2))
                .ToList();

            PopulateImportItems(preview, rawRows, activeItems);
            preview.ImportedCount = preview.Items.Count;

            return preview;
        }

        private static async Task<PurchaseImportPreviewVM> BuildPdfImportPreviewAsync(
            IFormFile file,
            IEnumerable<ItemMaster> activeItems,
            IEnumerable<Supplier> suppliers)
        {
            var pdfReadResult = await ReadPdfImportAsync(file);
            if (!pdfReadResult.Rows.Any())
            {
                throw new InvalidOperationException(
                    "No purchase item rows could be identified in the selected PDF. If this is a scanned/image PDF, OCR support is still needed.");
            }

            var preview = new PurchaseImportPreviewVM
            {
                DetectedFormat = GetDetectedImportFormat(file.FileName),
                Header = BuildPdfImportHeader(pdfReadResult, suppliers)
            };

            PopulateImportItems(preview, pdfReadResult.Rows, activeItems);
            preview.ImportedCount = preview.Items.Count;

            return preview;
        }

        // Header builders
        private static PurchaseImportHeaderVM BuildImportHeader(
            IReadOnlyList<IReadOnlyList<string>> matrix,
            int headerRowIndex,
            IReadOnlyList<Dictionary<string, string>> rowMaps,
            IEnumerable<Supplier> suppliers)
        {
            var header = new PurchaseImportHeaderVM();
            var firstDataRow = rowMaps.FirstOrDefault();

            var billNo = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportBillNoAliases);
            if (!string.IsNullOrWhiteSpace(billNo))
            {
                header.BillNo = billNo;
            }

            var billDateText = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportBillDateAliases);
            if (TryParseImportDate(billDateText, out var billDate))
            {
                header.BillDate = billDate;
            }

            var partyBillNo = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportPartyBillNoAliases);
            if (!string.IsNullOrWhiteSpace(partyBillNo))
            {
                header.PartyBillNo = partyBillNo;
            }

            var partyBillDateText = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportPartyBillDateAliases);
            if (TryParseImportDate(partyBillDateText, out var partyBillDate))
            {
                header.PartyBillDate = partyBillDate;
            }

            var purchaseType = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportPurchaseTypeAliases);
            if (!string.IsNullOrWhiteSpace(purchaseType))
            {
                header.PurchaseType = purchaseType.Trim().Equals("Central", StringComparison.OrdinalIgnoreCase)
                    ? "Central"
                    : "Local";
            }

            var supplierName = ResolveImportHeaderValue(matrix, headerRowIndex, firstDataRow, ImportSupplierAliases);
            ApplySupplierMatch(header, supplierName, suppliers);

            return header;
        }

        private static PurchaseImportHeaderVM BuildPdfImportHeader(
            PdfImportReadResult pdfReadResult,
            IEnumerable<Supplier> suppliers)
        {
            var header = new PurchaseImportHeaderVM
            {
                PurchaseType = InferPurchaseTypeFromText(pdfReadResult.FullText)
            };

            if (TryParseImportDate(ExtractLabeledDateValue(pdfReadResult.Lines, ImportBillDateAliases), out var billDate))
            {
                header.BillDate = billDate;
            }

            if (TryParseImportDate(ExtractLabeledDateValue(pdfReadResult.Lines, ImportPartyBillDateAliases), out var partyBillDate))
            {
                header.PartyBillDate = partyBillDate;
            }

            header.BillNo = ExtractLabeledTextValue(pdfReadResult.Lines, ImportBillNoAliases);
            header.PartyBillNo = ExtractLabeledTextValue(pdfReadResult.Lines, ImportPartyBillNoAliases);

            var supplierName = ExtractLabeledTextValue(pdfReadResult.Lines, ImportSupplierAliases);
            ApplySupplierMatch(header, supplierName, suppliers);

            return header;
        }

        private static void ApplySupplierMatch(
            PurchaseImportHeaderVM header,
            string? supplierName,
            IEnumerable<Supplier> suppliers)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
            {
                return;
            }

            header.SupplierName = supplierName.Trim();

            var normalizedSupplierName = NormalizeNameKey(header.SupplierName);
            var matchedSuppliers = suppliers
                .Where(x => NormalizeNameKey(x.FirstName) == normalizedSupplierName)
                .ToList();

            if (matchedSuppliers.Count == 1)
            {
                header.SupplierId = matchedSuppliers[0].Id;
            }
        }

        // Raw row readers
        private static void PopulateImportItems(
            PurchaseImportPreviewVM preview,
            IEnumerable<PurchaseImportRawRow> rawRows,
            IEnumerable<ItemMaster> activeItems)
        {
            var itemList = activeItems.ToList();
            var barcodeLookup = BuildItemLookup(itemList, x => x.Barcode, NormalizeIdentifierKey);
            var codeLookup = BuildItemLookup(itemList, x => x.Code, NormalizeIdentifierKey);
            var nameLookup = BuildItemLookup(itemList, x => x.Name, NormalizeNameKey);

            foreach (var rawRow in rawRows.OrderBy(x => x.RowNumber))
            {
                NormalizeImportRawRow(rawRow);

                if (IsImportRowBlank(rawRow) || ShouldIgnoreNonItemRow(rawRow))
                {
                    continue;
                }

                var rowWarnings = new List<string>();
                ItemMaster? item = null;
                string? matchError = null;

                if (HasImportItemIdentifier(rawRow))
                {
                    TryResolveImportedItem(rawRow, itemList, barcodeLookup, codeLookup, nameLookup, out item, out matchError);
                    if (!string.IsNullOrWhiteSpace(matchError))
                    {
                        rowWarnings.Add(matchError);
                    }
                }
                else
                {
                    rowWarnings.Add("Item name/code/barcode could not be identified from this row.");
                }

                var itemName = item?.Name
                    ?? CleanImportText(rawRow.ItemName)
                    ?? CleanImportText(rawRow.ItemCode)
                    ?? CleanImportText(rawRow.Barcode);

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    preview.Warnings.Add($"Row {rawRow.RowNumber}: Item name/code/barcode could not be identified from this row.");
                    continue;
                }

                var qty = ParseRequiredWholeNumberCell(rawRow.QtyText, "Qty", rowWarnings);
                var freeQty = ParseOptionalWholeNumberCell(rawRow.FreeQtyText, "Free Qty", rowWarnings);
                var amount = ParseOptionalDecimalCell(rawRow.AmountText, "Amount", rowWarnings);

                decimal rate;
                if (string.IsNullOrWhiteSpace(rawRow.RateText))
                {
                    if (qty > 0 && amount > 0)
                    {
                        rate = Math.Round(amount / qty, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        rowWarnings.Add("Rate is missing and needs manual entry.");
                        rate = 0m;
                    }
                }
                else
                {
                    rate = ParseRequiredPositiveDecimalCell(rawRow.RateText, "Rate", rowWarnings);
                }

                if (string.IsNullOrWhiteSpace(rawRow.Batch))
                {
                    rowWarnings.Add("Batch is missing and needs manual entry.");
                }

                DateTime? expiryDate = null;
                if (!string.IsNullOrWhiteSpace(rawRow.ExpiryText))
                {
                    if (TryParseImportDate(rawRow.ExpiryText, out var parsedExpiry))
                    {
                        expiryDate = parsedExpiry;
                    }
                    else
                    {
                        rowWarnings.Add($"Expiry '{rawRow.ExpiryText}' could not be understood.");
                    }
                }

                var mrp = ParseOptionalDecimalCell(rawRow.MrpText, "MRP", rowWarnings);
                var discount = ParseOptionalDecimalCell(rawRow.DiscountText, "Discount", rowWarnings);
                var salesRateA = ParseOptionalDecimalCell(rawRow.SalesRateAText, "Sales Rate A", rowWarnings);
                var salesRateB = ParseOptionalDecimalCell(rawRow.SalesRateBText, "Sales Rate B", rowWarnings);
                var gst = ParseOptionalDecimalCell(rawRow.GstText, "GST", rowWarnings);
                var cgst = ParseOptionalDecimalCell(rawRow.CGstText, "CGST", rowWarnings);
                var sgst = ParseOptionalDecimalCell(rawRow.SGstText, "SGST", rowWarnings);
                var cess = ParseOptionalDecimalCell(rawRow.CessText, "Cess", rowWarnings);
                var cessAmount = ParseOptionalDecimalCell(rawRow.CessAmountText, "Cess Amount", rowWarnings);

                if (item != null)
                {
                    mrp = mrp > 0 ? mrp : item.Mrp;
                    salesRateA = salesRateA > 0 ? salesRateA : item.SalesRate1;
                    salesRateB = salesRateB > 0 ? salesRateB : item.SalesRate2;

                    if (gst <= 0m && cgst <= 0m && sgst <= 0m)
                    {
                        gst = item.Hsn?.IGST ?? 0m;
                        cgst = item.Hsn?.CGST ?? 0m;
                        sgst = item.Hsn?.SGST ?? 0m;
                    }

                    if (cess <= 0m)
                    {
                        cess = item.Hsn?.Cess ?? 0m;
                    }
                }

                if (gst <= 0m && cgst > 0m && sgst > 0m)
                {
                    gst = cgst + sgst;
                }

                if (cess <= 0m && cessAmount > 0m && amount > 0m)
                {
                    cess = Math.Round((cessAmount / amount) * 100m, 2, MidpointRounding.AwayFromZero);
                }

                foreach (var warning in rowWarnings
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    preview.Warnings.Add($"Row {rawRow.RowNumber}: {warning}");
                }

                preview.Items.Add(new PurchaseImportItemVM
                {
                    ItemId = item?.Id,
                    ItemName = itemName,
                    Batch = rawRow.Batch ?? string.Empty,
                    ExpiryDate = expiryDate?.ToString("yyyy-MM-dd"),
                    Qty = qty,
                    FreeQty = freeQty,
                    Rate = rate,
                    Mrp = mrp,
                    Discount = discount,
                    Gst = gst,
                    CGst = cgst,
                    SGst = sgst,
                    SCGst = cgst + sgst,
                    Cess = cess,
                    HsnId = item?.HsnId,
                    SalserateA = salesRateA,
                    SalserateB = salesRateB,
                    BatchWiseCose = 0m,
                    Barcode = rawRow.Barcode ?? item?.Barcode,
                    IsResolved = item != null,
                    ResolutionMessage = item == null ? matchError : null
                });
            }
        }

        private static PurchaseImportRawRow MapPurchaseImportRawRow(
            IReadOnlyDictionary<string, string> row,
            int rowNumber)
        {
            return new PurchaseImportRawRow
            {
                RowNumber = rowNumber,
                ItemName = GetImportCellValue(row, ImportItemNameAliases),
                ItemCode = GetImportCellValue(row, ImportItemCodeAliases),
                Barcode = GetImportCellValue(row, ImportBarcodeAliases),
                PackText = GetImportCellValue(row, ImportPackAliases),
                Batch = GetImportCellValue(row, ImportBatchAliases),
                ExpiryText = GetImportCellValue(row, ImportExpiryAliases),
                QtyText = GetImportCellValue(row, ImportQtyAliases),
                FreeQtyText = GetImportCellValue(row, ImportFreeQtyAliases),
                RateText = GetImportCellValue(row, ImportRateAliases),
                MrpText = GetImportCellValue(row, ImportMrpAliases),
                DiscountText = GetImportCellValue(row, ImportDiscountAliases),
                GstText = GetImportCellValue(row, ImportGstAliases),
                CGstText = GetImportCellValue(row, ImportCgstAliases),
                SGstText = GetImportCellValue(row, ImportSgstAliases),
                CessText = GetImportCellValue(row, ImportCessAliases),
                CessAmountText = GetImportCellValue(row, ImportCessAmountAliases),
                AmountText = GetImportCellValue(row, ImportAmountAliases),
                SalesRateAText = GetImportCellValue(row, ImportSalesRateAAliases),
                SalesRateBText = GetImportCellValue(row, ImportSalesRateBAliases),
                RawText = string.Join(" | ", row
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => $"{x.Key}: {x.Value}"))
            };
        }

        // PDF readers
        private static async Task<PdfImportReadResult> ReadPdfImportAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = PdfDocument.Open(memoryStream);
            var result = new PdfImportReadResult();
            var hasSelectableText = false;
            var lineNumberOffset = 0;

            foreach (var page in document.GetPages())
            {
                var lines = ExtractPdfLines(page);
                if (lines.Count > 0)
                {
                    hasSelectableText = true;
                    result.Lines.AddRange(lines.Select(x => x.Text));
                }

                var headerLineIndex = FindPdfHeaderLineIndex(lines);
                if (headerLineIndex < 0)
                {
                    lineNumberOffset += lines.Count;
                    continue;
                }

                var columns = BuildPdfColumnBoundaries(lines[headerLineIndex]);
                if (!columns.Any())
                {
                    lineNumberOffset += lines.Count;
                    continue;
                }

                PurchaseImportRawRow? lastRow = null;
                PdfLineRow? lastDataLine = null;

                for (var lineIndex = headerLineIndex + 1; lineIndex < lines.Count; lineIndex++)
                {
                    var line = lines[lineIndex];
                    if (IsPdfFooterLine(line.Text) && result.Rows.Any())
                    {
                        break;
                    }

                    var mappedRow = MapPdfImportRawRow(line, columns, lineNumberOffset + lineIndex + 1);
                    NormalizeImportRawRow(mappedRow);

                    if (TryMergePdfContinuationRow(lastRow, lastDataLine, mappedRow, line))
                    {
                        continue;
                    }

                    if (!LooksLikePdfItemRow(mappedRow))
                    {
                        continue;
                    }

                    result.Rows.Add(mappedRow);
                    lastRow = mappedRow;
                    lastDataLine = line;
                }

                lineNumberOffset += lines.Count;
            }

            if (!hasSelectableText)
            {
                throw new InvalidOperationException("The selected PDF appears to be scanned or image-based. Text import currently works with selectable-text PDF, Excel, and CSV files.");
            }

            return result;
        }

        private static List<PdfLineRow> ExtractPdfLines(UglyToad.PdfPig.Content.Page page)
        {
            var words = page.GetWords()
                .Select(x => new PdfWordCell(CleanImportText(x.Text) ?? string.Empty, x.BoundingBox.Left, x.BoundingBox.Bottom))
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .OrderByDescending(x => x.Y)
                .ThenBy(x => x.X)
                .ToList();

            var lines = new List<PdfLineRow>();

            foreach (var word in words)
            {
                var currentLine = lines.LastOrDefault();
                if (currentLine == null || Math.Abs(currentLine.Y - word.Y) > PdfLineTolerance)
                {
                    currentLine = new PdfLineRow(page.Number, word.Y);
                    lines.Add(currentLine);
                }

                currentLine.Words.Add(word);
            }

            foreach (var line in lines)
            {
                line.Words.Sort((left, right) => left.X.CompareTo(right.X));
                line.Text = string.Join(" ", line.Words.Select(x => x.Text));
            }

            return lines;
        }

        private static int FindPdfHeaderLineIndex(IReadOnlyList<PdfLineRow> lines)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                var text = NormalizeHeaderKey(lines[index].Text);

                // 🟢 BTR FIX: 'BATCH' is no longer strictly required in the header line.
                // Just looking for QTY and ITEM identifier and RATE/AMOUNT.
                if (text.Contains("QTY", StringComparison.Ordinal) &&
                    (text.Contains("PRODUCT", StringComparison.Ordinal) ||
                     text.Contains("ITEM", StringComparison.Ordinal) ||
                     text.Contains("MEDICINE", StringComparison.Ordinal) ||
                     text.Contains("DESCRIPTION", StringComparison.Ordinal) || // Added DESCRIPTION
                     text.Contains("NAME", StringComparison.Ordinal)) &&
                    (text.Contains("RATE", StringComparison.Ordinal) ||
                     text.Contains("MRP", StringComparison.Ordinal) ||
                     text.Contains("AMOUNT", StringComparison.Ordinal)))
                {
                    return index;
                }
            }

            return -1;
        }

        private static List<PdfColumnBoundary> BuildPdfColumnBoundaries(PdfLineRow headerLine)
        {
            var markers = new List<(string Key, double X)>();

            AddPdfMarker(markers, "serial", FindPdfHeaderStart(headerLine, "S", "SR", "SL", "NO", "SNO"));
            AddPdfMarker(markers, "qty", FindPdfHeaderStart(headerLine, "QTY", "QUANTITY"));
            AddPdfMarker(markers, "mfr", FindPdfHeaderStart(headerLine, "MFR", "MFG", "MANUFACTURER"));
            AddPdfMarker(markers, "pack", FindPdfHeaderStart(headerLine, "PACK", "PACKING", "SIZE"));
            AddPdfMarker(markers, "item", FindPdfHeaderStart(headerLine, "PRODUCT", "ITEM", "MEDICINE", "DESCRIPTION", "PARTICULAR", "NAME"));
            AddPdfMarker(markers, "batch", FindPdfHeaderStart(headerLine, "BATCH"));
            AddPdfMarker(markers, "exp", FindPdfHeaderStart(headerLine, "EXP", "EXPIRY", "EXPIRYDATE"));
            AddPdfMarker(markers, "hsn", FindPdfHeaderStart(headerLine, "HSN"));
            AddPdfMarker(markers, "mrp", FindPdfHeaderStart(headerLine, "MRP"));
            AddPdfMarker(markers, "rate", FindPdfHeaderStart(headerLine, "RATE", "PTR"));
            AddPdfMarker(markers, "discount", FindPdfHeaderStart(headerLine, "DIS", "DISC", "DISCOUNT"));
            AddPdfMarker(markers, "sgst", FindPdfHeaderStart(headerLine, "SGST"));
            AddPdfMarker(markers, "cgst", FindPdfHeaderStart(headerLine, "CGST"));
            AddPdfMarker(markers, "gst", FindPdfHeaderStart(headerLine, "GST", "IGST", "TAX"));
            AddPdfMarker(markers, "cess", FindPdfHeaderStart(headerLine, "CESS"));
            AddPdfMarker(markers, "amount", FindPdfHeaderStart(headerLine, "AMOUNT"));
            AddPdfMarker(markers, "net", FindPdfHeaderStart(headerLine, "NET"));

            var orderedMarkers = markers.OrderBy(x => x.X).ToList();
            var columns = new List<PdfColumnBoundary>();

            for (var index = 0; index < orderedMarkers.Count; index++)
            {
                var marker = orderedMarkers[index];
                var startX = index == 0
                    ? double.MinValue
                    : marker.X - PdfColumnSnapTolerance;
                var endX = index == orderedMarkers.Count - 1
                    ? double.MaxValue
                    : orderedMarkers[index + 1].X - PdfColumnSnapTolerance;

                columns.Add(new PdfColumnBoundary(marker.Key, startX, endX));
            }

            return columns;
        }

        private static void AddPdfMarker(List<(string Key, double X)> markers, string key, double? x)
        {
            if (x.HasValue)
            {
                markers.Add((key, x.Value));
            }
        }

        private static double? FindPdfHeaderStart(PdfLineRow headerLine, params string[] aliases)
        {
            var normalizedAliases = aliases
                .Select(NormalizeHeaderKey)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return headerLine.Words
                .Where(x => normalizedAliases.Contains(NormalizeHeaderKey(x.Text)))
                .OrderBy(x => x.X)
                .Select(x => (double?)x.X)
                .FirstOrDefault();
        }

        private static PurchaseImportRawRow MapPdfImportRawRow(
            PdfLineRow line,
            IReadOnlyList<PdfColumnBoundary> columns,
            int rowNumber)
        {
            var manufacturer = ReadPdfColumnValue(line, columns, "mfr");
            var pack = ReadPdfColumnValue(line, columns, "pack");

            return new PurchaseImportRawRow
            {
                RowNumber = rowNumber,
                ItemName = ReadPdfColumnValue(line, columns, "item"),
                PackText = JoinNonEmptyValues(manufacturer, pack),
                Batch = ReadPdfColumnValue(line, columns, "batch"),
                ExpiryText = ReadPdfColumnValue(line, columns, "exp"),
                QtyText = ReadPdfColumnValue(line, columns, "qty"),
                RateText = ReadPdfColumnValue(line, columns, "rate"),
                MrpText = ReadPdfColumnValue(line, columns, "mrp"),
                DiscountText = ReadPdfColumnValue(line, columns, "discount"),
                GstText = ReadPdfColumnValue(line, columns, "gst"),
                CGstText = ReadPdfColumnValue(line, columns, "cgst"),
                SGstText = ReadPdfColumnValue(line, columns, "sgst"),
                CessText = ReadPdfColumnValue(line, columns, "cess"),
                AmountText = ReadPdfColumnValue(line, columns, "amount") ?? ReadPdfColumnValue(line, columns, "net"),
                RawText = line.Text
            };
        }

        private static string? ReadPdfColumnValue(
            PdfLineRow line,
            IReadOnlyList<PdfColumnBoundary> columns,
            string key)
        {
            var column = columns.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (column == null)
            {
                return null;
            }

            var text = string.Join(" ", line.Words
                .Where(x => x.X >= column.StartX && x.X < column.EndX)
                .Select(x => x.Text));

            return CleanImportText(text);
        }

        private static bool TryMergePdfContinuationRow(
            PurchaseImportRawRow? lastRow,
            PdfLineRow? lastDataLine,
            PurchaseImportRawRow currentRow,
            PdfLineRow currentLine)
        {
            if (lastRow == null || lastDataLine == null)
            {
                return false;
            }

            if (Math.Abs(lastDataLine.Y - currentLine.Y) > PdfContinuationTolerance)
            {
                return false;
            }

            var hasCoreShape = !string.IsNullOrWhiteSpace(currentRow.QtyText)
                || !string.IsNullOrWhiteSpace(currentRow.Batch)
                || !string.IsNullOrWhiteSpace(currentRow.RateText)
                || !string.IsNullOrWhiteSpace(currentRow.MrpText);

            if (!hasCoreShape && !string.IsNullOrWhiteSpace(currentRow.ItemName))
            {
                lastRow.ItemName = JoinNonEmptyValues(lastRow.ItemName, currentRow.ItemName);
                return true;
            }

            var isValueOnlyContinuation = string.IsNullOrWhiteSpace(currentRow.ItemName)
                && string.IsNullOrWhiteSpace(currentRow.Batch)
                && string.IsNullOrWhiteSpace(currentRow.QtyText)
                && string.IsNullOrWhiteSpace(currentRow.RateText)
                && !string.IsNullOrWhiteSpace(currentRow.AmountText);

            if (isValueOnlyContinuation)
            {
                if (string.IsNullOrWhiteSpace(lastRow.AmountText))
                {
                    lastRow.AmountText = currentRow.AmountText;
                }

                return true;
            }

            if (string.IsNullOrWhiteSpace(lastRow.Batch) && !string.IsNullOrWhiteSpace(currentRow.Batch))
            {
                lastRow.Batch = currentRow.Batch;
                lastRow.ExpiryText = lastRow.ExpiryText ?? currentRow.ExpiryText;
                lastRow.RateText = lastRow.RateText ?? currentRow.RateText;
                lastRow.MrpText = lastRow.MrpText ?? currentRow.MrpText;
                lastRow.AmountText = lastRow.AmountText ?? currentRow.AmountText;
                return true;
            }

            return false;
        }

        private static bool LooksLikePdfItemRow(PurchaseImportRawRow row)
        {
            if (ShouldIgnoreNonItemRow(row))
            {
                return false;
            }

            var hasIdentifier = HasImportItemIdentifier(row) || !string.IsNullOrWhiteSpace(row.Batch);
            var hasRowShape = !string.IsNullOrWhiteSpace(row.QtyText)
                || !string.IsNullOrWhiteSpace(row.Batch)
                || !string.IsNullOrWhiteSpace(row.RateText)
                || !string.IsNullOrWhiteSpace(row.MrpText);

            return hasIdentifier && hasRowShape;
        }

        private static bool IsPdfFooterLine(string? lineText)
        {
            if (string.IsNullOrWhiteSpace(lineText))
            {
                return false;
            }

            var normalized = NormalizeHeaderKey(lineText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return SummaryRowContainsKeywords.Any(normalized.Contains)
                || normalized.StartsWith("GST", StringComparison.Ordinal)
                || normalized.StartsWith("SGST", StringComparison.Ordinal)
                || normalized.StartsWith("CGST", StringComparison.Ordinal)
                || normalized.StartsWith("IGST", StringComparison.Ordinal);
        }

        // Structured readers
        private static async Task<List<List<string>>> ReadImportMatrixAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            return extension switch
            {
                ".xlsx" or ".xls" => await ReadSpreadsheetMatrixAsync(file),
                ".csv" => await ReadCsvMatrixAsync(file),
                ".pdf" => await ReadPdfMatrixAsync(file),
                ".jpg" or ".jpeg" or ".png" => await ReadImageMatrixWithOcrAsync(file), // 🟢 NEW
                _ => throw new InvalidOperationException("Supported import files are .xlsx, .xls, .csv, .pdf, and images (.jpg, .png).")
            };
        }

        private static async Task<List<List<string>>> ReadSpreadsheetMatrixAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            try
            {
                var workbook = WorkbookFactory.Create(stream);
                if (workbook.NumberOfSheets == 0)
                {
                    throw new InvalidOperationException("Excel file does not contain any worksheet.");
                }

                var sheet = workbook.GetSheetAt(0);
                var matrix = new List<List<string>>();

                for (var rowIndex = sheet.FirstRowNum; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null || row.LastCellNum < 0)
                    {
                        matrix.Add(new List<string>());
                        continue;
                    }

                    var cells = new List<string>();
                    for (var cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                    {
                        cells.Add(row.GetCell(cellIndex)?.ToString()?.Trim() ?? string.Empty);
                    }

                    matrix.Add(cells);
                }

                return await Task.FromResult(matrix);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException("Excel file could not be read. Please use a valid .xls or .xlsx file.");
            }
        }

        private static async Task<List<List<string>>> ReadCsvMatrixAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var reader = new StreamReader(memoryStream, Encoding.UTF8, true, leaveOpen: true);
            var firstLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                throw new InvalidOperationException("CSV file is blank.");
            }

            memoryStream.Position = 0;
            var delimiter = DetectDelimitedSeparator(firstLine);
            var matrix = new List<List<string>>();

            using var parser = new TextFieldParser(memoryStream, Encoding.UTF8, true)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };

            parser.SetDelimiters(delimiter);

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                matrix.Add(fields.Select(x => x?.Trim() ?? string.Empty).ToList());
            }

            return matrix;
        }

        private static int FindImportHeaderRowIndex(IReadOnlyList<IReadOnlyList<string>> matrix)
        {
            for (var index = 0; index < matrix.Count; index++)
            {
                var row = matrix[index];
                if (!row.Any())
                {
                    continue;
                }

                var hasItemIdentifier = row.Any(cell =>
                    MatchesAlias(cell, ImportItemNameAliases) ||
                    MatchesAlias(cell, ImportItemCodeAliases) ||
                    MatchesAlias(cell, ImportBarcodeAliases));

                var hasQuantityOrRate = row.Any(cell =>
                    MatchesAlias(cell, ImportQtyAliases) ||
                    MatchesAlias(cell, ImportRateAliases) ||
                    MatchesAlias(cell, ImportBatchAliases));

                if (hasItemIdentifier && hasQuantityOrRate)
                {
                    return index;
                }
            }

            return -1;
        }

        private static List<Dictionary<string, string>> ConvertImportRowsToDictionaries(
            IReadOnlyList<IReadOnlyList<string>> matrix,
            int headerRowIndex)
        {
            var headers = matrix[headerRowIndex]
                .Select((cell, index) => string.IsNullOrWhiteSpace(cell) ? $"Column{index + 1}" : cell.Trim())
                .ToList();

            var rows = new List<Dictionary<string, string>>();

            for (var rowIndex = headerRowIndex + 1; rowIndex < matrix.Count; rowIndex++)
            {
                var row = matrix[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var rowMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var columnIndex = 0; columnIndex < headers.Count; columnIndex++)
                {
                    rowMap[headers[columnIndex]] = columnIndex < row.Count
                        ? row[columnIndex]?.Trim() ?? string.Empty
                        : string.Empty;
                }

                rows.Add(rowMap);
            }

            return rows;
        }

        private static string? ResolveImportHeaderValue(
            IReadOnlyList<IReadOnlyList<string>> matrix,
            int headerRowIndex,
            IReadOnlyDictionary<string, string>? firstDataRow,
            string[] aliases)
        {
            var metadataValue = FindMetadataValue(matrix, headerRowIndex, aliases);
            if (!string.IsNullOrWhiteSpace(metadataValue))
            {
                return metadataValue;
            }

            return firstDataRow == null ? null : GetImportCellValue(firstDataRow, aliases);
        }

        private static string? FindMetadataValue(
            IReadOnlyList<IReadOnlyList<string>> matrix,
            int headerRowIndex,
            string[] aliases)
        {
            for (var rowIndex = 0; rowIndex < headerRowIndex; rowIndex++)
            {
                var row = matrix[rowIndex];
                for (var columnIndex = 0; columnIndex < row.Count - 1; columnIndex++)
                {
                    if (MatchesAlias(row[columnIndex], aliases))
                    {
                        var value = row[columnIndex + 1]?.Trim();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }

            return null;
        }

        private static string? GetImportCellValue(
            IReadOnlyDictionary<string, string> row,
            string[] aliases)
        {
            foreach (var entry in row)
            {
                if (MatchesAlias(entry.Key, aliases) && !string.IsNullOrWhiteSpace(entry.Value))
                {
                    return entry.Value.Trim();
                }
            }

            return null;
        }

        // Matching and parsing helpers
        private static bool IsImportRowBlank(PurchaseImportRawRow row)
        {
            return string.IsNullOrWhiteSpace(row.ItemName) &&
                   string.IsNullOrWhiteSpace(row.ItemCode) &&
                   string.IsNullOrWhiteSpace(row.Barcode) &&
                   string.IsNullOrWhiteSpace(row.Batch) &&
                   string.IsNullOrWhiteSpace(row.QtyText) &&
                   string.IsNullOrWhiteSpace(row.RateText);
        }

        private static bool ShouldIgnoreNonItemRow(PurchaseImportRawRow row)
        {
            if (LooksLikeRepeatedHeaderRow(row))
            {
                return true;
            }

            if (IsLikelySummaryLabel(row.ItemName) || IsLikelySummaryLabel(row.ItemCode))
            {
                return true;
            }

            var normalizedRaw = NormalizeHeaderKey(row.RawText);
            if (!string.IsNullOrWhiteSpace(normalizedRaw) &&
                SummaryRowContainsKeywords.Any(normalizedRaw.Contains) &&
                string.IsNullOrWhiteSpace(row.Batch) &&
                string.IsNullOrWhiteSpace(row.QtyText) &&
                string.IsNullOrWhiteSpace(row.RateText))
            {
                return true;
            }

            return false;
        }

        private static bool LooksLikeRepeatedHeaderRow(PurchaseImportRawRow row)
        {
            return MatchesAlias(row.ItemName, ImportItemNameAliases)
                || MatchesAlias(row.ItemCode, ImportItemCodeAliases)
                || MatchesAlias(row.Batch, ImportBatchAliases)
                || MatchesAlias(row.QtyText, ImportQtyAliases)
                || MatchesAlias(row.RateText, ImportRateAliases);
        }

        private static bool IsLikelySummaryLabel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = NormalizeNameKey(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            if (NonItemLabels.Contains(normalized))
            {
                return true;
            }

            return SummaryRowContainsKeywords.Any(keyword => normalized.Replace(" ", string.Empty).Contains(keyword, StringComparison.Ordinal));
        }

        private static bool HasImportItemIdentifier(PurchaseImportRawRow row)
        {
            return !string.IsNullOrWhiteSpace(row.ItemName)
                || !string.IsNullOrWhiteSpace(row.ItemCode)
                || !string.IsNullOrWhiteSpace(row.Barcode);
        }

        private static void NormalizeImportRawRow(PurchaseImportRawRow row)
        {
            row.ItemName = CleanImportedItemName(row.ItemName);
            row.ItemCode = CleanImportText(row.ItemCode);
            row.Barcode = CleanImportText(row.Barcode);
            row.PackText = CleanImportText(row.PackText);
            row.Batch = CleanImportText(row.Batch)?.Replace(" ", string.Empty);
            row.ExpiryText = CleanImportText(row.ExpiryText);
            row.QtyText = CleanImportText(row.QtyText);
            row.FreeQtyText = CleanImportText(row.FreeQtyText);
            row.RateText = CleanImportText(row.RateText);
            row.MrpText = CleanImportText(row.MrpText);
            row.DiscountText = CleanImportText(row.DiscountText);
            row.GstText = CleanImportText(row.GstText);
            row.CGstText = CleanImportText(row.CGstText);
            row.SGstText = CleanImportText(row.SGstText);
            row.CessText = CleanImportText(row.CessText);
            row.CessAmountText = CleanImportText(row.CessAmountText);
            row.AmountText = CleanImportText(row.AmountText);
            row.SalesRateAText = CleanImportText(row.SalesRateAText);
            row.SalesRateBText = CleanImportText(row.SalesRateBText);
            row.RawText = CleanImportText(row.RawText) ?? string.Empty;

            SplitSchemeQuantity(row);
            SplitBatchAndExpiry(row);
        }

        private static void SplitSchemeQuantity(PurchaseImportRawRow row)
        {
            if (string.IsNullOrWhiteSpace(row.QtyText))
            {
                return;
            }

            var match = Regex.Match(row.QtyText, @"^(?<qty>\d+(?:\.\d+)?)\s*\+\s*(?<free>\d+(?:\.\d+)?)$");
            if (!match.Success)
            {
                return;
            }

            row.QtyText = match.Groups["qty"].Value;
            if (string.IsNullOrWhiteSpace(row.FreeQtyText))
            {
                row.FreeQtyText = match.Groups["free"].Value;
            }
        }

        private static void SplitBatchAndExpiry(PurchaseImportRawRow row)
        {
            if (string.IsNullOrWhiteSpace(row.Batch) || !string.IsNullOrWhiteSpace(row.ExpiryText))
            {
                return;
            }

            var batchText = row.Batch.Replace(" ", string.Empty);
            var match = Regex.Match(batchText, @"^(?<batch>.+?)(?:EXP)?(?<expiry>(0?[1-9]|1[0-2])[\/\-](\d{2}|\d{4}))$", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return;
            }

            row.Batch = match.Groups["batch"].Value;
            row.ExpiryText = match.Groups["expiry"].Value;
        }

        private static bool TryResolveImportedItem(
            PurchaseImportRawRow row,
            IReadOnlyList<ItemMaster> items,
            IReadOnlyDictionary<string, List<ItemMaster>> barcodeLookup,
            IReadOnlyDictionary<string, List<ItemMaster>> codeLookup,
            IReadOnlyDictionary<string, List<ItemMaster>> nameLookup,
            out ItemMaster? item,
            out string? error)
        {
            item = null;
            error = null;
            var errors = new List<string>();

            if (TryGetLookupMatch(barcodeLookup, row.Barcode, "Barcode", NormalizeIdentifierKey, out item, out var barcodeError))
            {
                return true;
            }
            if (!string.IsNullOrWhiteSpace(barcodeError))
            {
                errors.Add(barcodeError);
            }

            if (TryGetLookupMatch(codeLookup, row.ItemCode, "Item Code", NormalizeIdentifierKey, out item, out var codeError))
            {
                return true;
            }
            if (!string.IsNullOrWhiteSpace(codeError))
            {
                errors.Add(codeError);
            }

            if (TryGetLookupMatch(nameLookup, row.ItemName, "Item Name", NormalizeNameKey, out item, out var nameError))
            {
                return true;
            }
            if (!string.IsNullOrWhiteSpace(nameError))
            {
                errors.Add(nameError);
            }

            if (TryGetFuzzyNameMatch(items, row.ItemName, out item, out var fuzzyError))
            {
                return true;
            }
            if (!string.IsNullOrWhiteSpace(fuzzyError))
            {
                errors.Add(fuzzyError);
            }

            error = errors.FirstOrDefault()
                ?? "Item could not be matched. Use Barcode, Item Code, or exact/close Item Name.";
            return false;
        }

        private static bool TryGetLookupMatch(
            IReadOnlyDictionary<string, List<ItemMaster>> lookup,
            string? rawValue,
            string label,
            Func<string?, string> normalizer,
            out ItemMaster? item,
            out string? error)
        {
            item = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var key = normalizer(rawValue);
            if (!lookup.TryGetValue(key, out var matches))
            {
                error = $"{label} '{rawValue}' was not found in Item Master.";
                return false;
            }

            if (matches.Count != 1)
            {
                error = $"{label} '{rawValue}' matched multiple active items.";
                return false;
            }

            item = matches[0];
            return true;
        }

        private static bool TryGetFuzzyNameMatch(
            IReadOnlyList<ItemMaster> items,
            string? rawValue,
            out ItemMaster? item,
            out string? error)
        {
            item = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            var normalizedInput = NormalizeNameKey(rawValue);
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                return false;
            }

            var importedTokens = TokenizeNameKey(normalizedInput);
            if (importedTokens.Count == 0)
            {
                return false;
            }

            var matches = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => new
                {
                    Item = x,
                    NormalizedName = NormalizeNameKey(x.Name),
                    Score = ScoreItemNameMatch(normalizedInput, importedTokens, NormalizeNameKey(x.Name))
                })
                .Where(x => x.Score >= 0.72m)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => Math.Abs(x.NormalizedName.Length - normalizedInput.Length))
                .ToList();

            if (matches.Count == 0)
            {
                return false;
            }

            if (matches.Count > 1 && matches[1].Score >= matches[0].Score - 0.05m)
            {
                error = $"Item Name '{rawValue}' matched multiple similar items.";
                return false;
            }

            item = matches[0].Item;
            return true;
        }

        private static decimal ScoreItemNameMatch(
            string normalizedInput,
            HashSet<string> importedTokens,
            string normalizedCandidate)
        {
            if (string.IsNullOrWhiteSpace(normalizedCandidate))
            {
                return 0m;
            }

            if (normalizedInput.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            var candidateTokens = TokenizeNameKey(normalizedCandidate);
            if (candidateTokens.Count == 0)
            {
                return 0m;
            }

            var importedNumbers = importedTokens.Where(IsNumericToken).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidateNumbers = candidateTokens.Where(IsNumericToken).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (importedNumbers.Count > 0 && candidateNumbers.Count > 0 && !importedNumbers.SetEquals(candidateNumbers))
            {
                return 0.35m;
            }

            if (normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase) ||
                normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                var shorterLength = Math.Min(normalizedInput.Length, normalizedCandidate.Length);
                var longerLength = Math.Max(normalizedInput.Length, normalizedCandidate.Length);
                return 0.90m + ((decimal)shorterLength / longerLength * 0.05m);
            }

            var overlapCount = importedTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
            if (overlapCount == 0)
            {
                return 0m;
            }

            var unionCount = importedTokens.Union(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
            var tokenScore = unionCount == 0 ? 0m : (decimal)overlapCount / unionCount;

            if (overlapCount >= 3 && tokenScore >= 0.60m)
            {
                return Math.Min(0.89m, tokenScore + 0.18m);
            }

            if (overlapCount >= 2 && tokenScore >= 0.50m)
            {
                return Math.Min(0.84m, tokenScore + 0.15m);
            }

            return tokenScore;
        }

        private static bool IsNumericToken(string token)
        {
            return token.All(char.IsDigit);
        }

        private static HashSet<string> TokenizeNameKey(string normalizedValue)
        {
            return normalizedValue
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token.Length > 1 || token.Any(char.IsDigit))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, List<ItemMaster>> BuildItemLookup(
            IEnumerable<ItemMaster> items,
            Func<ItemMaster, string?> selector,
            Func<string?, string> normalizer)
        {
            return items
                .Select(item => new
                {
                    Item = item,
                    Key = normalizer(selector(item))
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Item).ToList(),
                    StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeIdentifierKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value.Trim().ToUpperInvariant(), @"[\s\-_\/\\]", string.Empty);
        }

        private static string NormalizeNameKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ");
            return Regex.Replace(normalized, @"\s+", " ").Trim();
        }

        private static bool MatchesAlias(string? value, IEnumerable<string> aliases)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalizedValue = NormalizeHeaderKey(value);
            return aliases.Any(alias => NormalizeHeaderKey(alias) == normalizedValue);
        }

        private static string NormalizeHeaderKey(string value)
        {
            return Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);
        }

        private static int ParseRequiredWholeNumberCell(string? value, string label, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                warnings.Add($"{label} is missing and needs manual entry.");
                return 0;
            }

            if (!TryParseImportWholeNumber(value, out var result) || result <= 0)
            {
                warnings.Add($"{label} '{value}' could not be understood.");
                return 0;
            }

            return result;
        }

        private static int ParseOptionalWholeNumberCell(string? value, string label, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            if (!TryParseImportWholeNumber(value, out var result) || result < 0)
            {
                warnings.Add($"{label} '{value}' could not be understood.");
                return 0;
            }

            return result;
        }

        private static decimal ParseRequiredPositiveDecimalCell(string? value, string label, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                warnings.Add($"{label} is missing and needs manual entry.");
                return 0m;
            }

            if (!TryParseImportDecimal(value, out var result) || result <= 0m)
            {
                warnings.Add($"{label} '{value}' must be greater than 0.");
                return 0m;
            }

            return result;
        }

        private static decimal ParseOptionalDecimalCell(string? value, string label, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0m;
            }

            if (!TryParseImportDecimal(value, out var result))
            {
                warnings.Add($"{label} '{value}' could not be understood.");
                return 0m;
            }

            return result;
        }

        private static bool TryParseImportWholeNumber(string? value, out int result)
        {
            result = 0;
            if (!TryParseImportDecimal(value, out var decimalValue))
            {
                return false;
            }

            if (decimalValue != Math.Truncate(decimalValue))
            {
                return false;
            }

            result = (int)decimalValue;
            return true;
        }

        private static bool TryParseImportDecimal(string? value, out decimal result)
        {
            result = 0m;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var sanitizedValue = value.Trim()
                .Replace("%", string.Empty)
                .Replace(",", string.Empty);

            return decimal.TryParse(sanitizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
                || decimal.TryParse(sanitizedValue, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
        }

        private static bool TryParseImportDate(string? value, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmedValue = value.Trim();

            if (double.TryParse(trimmedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var excelDate) &&
                excelDate > 0 &&
                excelDate < 60000)
            {
                result = DateTime.FromOADate(excelDate).Date;
                return true;
            }

            var formats = new[]
            {
                "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                "dd/MM/yy", "d/M/yy", "dd-MM-yy", "d-M-yy",
                "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd", "dd MMM yyyy",
                "d MMM yyyy", "MMM yyyy", "MM/yyyy", "M/yyyy",
                "MM-yyyy", "M-yyyy", "MM/yy", "M/yy", "MM-yy", "M-yy",
                "MMM-yy", "MMM/yyyy", "MMM yy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(trimmedValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    if (format.Contains("MMM yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MM/yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("M/yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MM-yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("M-yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MM/yy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("M/yy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MM-yy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("M-yy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MMM-yy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MMM/yyyy", StringComparison.OrdinalIgnoreCase) ||
                        format.Contains("MMM yy", StringComparison.OrdinalIgnoreCase))
                    {
                        parsedDate = new DateTime(parsedDate.Year, parsedDate.Month, DateTime.DaysInMonth(parsedDate.Year, parsedDate.Month));
                    }

                    result = parsedDate.Date;
                    return true;
                }
            }

            return DateTime.TryParse(trimmedValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result)
                || DateTime.TryParse(trimmedValue, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result);
        }

        private static string DetectDelimitedSeparator(string firstLine)
        {
            var delimiterScores = new Dictionary<string, int>
            {
                [","] = firstLine.Count(x => x == ','),
                [";"] = firstLine.Count(x => x == ';'),
                ["\t"] = firstLine.Count(x => x == '\t'),
                ["|"] = firstLine.Count(x => x == '|')
            };

            return delimiterScores
                .OrderByDescending(x => x.Value)
                .First().Key;
        }

        private static string GetDetectedImportFormat(string fileName)
        {
            return Path.GetExtension(fileName)?.ToLowerInvariant() switch
            {
                ".xlsx" or ".xls" => "Excel",
                ".csv" => "CSV",
                ".pdf" => "PDF",
                _ => "Unknown"
            };
        }

        private static string? CleanImportText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return Regex.Replace(value.Trim(), @"\s+", " ");
        }

        private static string? CleanImportedItemName(string? value)
        {
            var cleaned = CleanImportText(value);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return null;
            }

            cleaned = Regex.Replace(cleaned, @"\s+\([A-Z0-9]{1,2}$", string.Empty, RegexOptions.IgnoreCase);
            return CleanImportText(cleaned);
        }

        private static string? JoinNonEmptyValues(params string?[] values)
        {
            var parts = values
                .Select(CleanImportText)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return parts.Count == 0
                ? null
                : string.Join(" ", parts);
        }

        private static string InferPurchaseTypeFromText(string? fullText)
        {
            var normalized = NormalizeHeaderKey(fullText ?? string.Empty);
            if (normalized.Contains("IGST", StringComparison.Ordinal) &&
                !normalized.Contains("CGST", StringComparison.Ordinal) &&
                !normalized.Contains("SGST", StringComparison.Ordinal))
            {
                return "Central";
            }

            return "Local";
        }

        private static string? ExtractLabeledDateValue(IEnumerable<string> lines, IEnumerable<string> aliases)
        {
            foreach (var line in lines.Take(30))
            {
                foreach (var alias in aliases)
                {
                    if (line.IndexOf(alias, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    var dateMatch = Regex.Match(line, @"\b\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}\b|\b\d{4}[\/\-]\d{1,2}[\/\-]\d{1,2}\b|\b\d{1,2}[\/\-]\d{2}\b");
                    if (dateMatch.Success)
                    {
                        return dateMatch.Value;
                    }
                }
            }

            return null;
        }

        private static string? ExtractLabeledTextValue(IEnumerable<string> lines, IEnumerable<string> aliases)
        {
            foreach (var line in lines.Take(30))
            {
                foreach (var alias in aliases)
                {
                    var index = line.IndexOf(alias, StringComparison.OrdinalIgnoreCase);
                    if (index < 0)
                    {
                        continue;
                    }

                    var value = line[(index + alias.Length)..]
                        .Trim()
                        .TrimStart(':', '-', '#')
                        .Trim();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            return null;
        }

        private static async Task<List<List<string>>> ReadPdfMatrixAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var document = PdfDocument.Open(memoryStream);
            var matrix = new List<List<string>>();

            foreach (var page in document.GetPages())
            {
                var lines = page.GetWords()
                    .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                    .OrderByDescending(g => g.Key)
                    .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))
                    .ToList();

                foreach (var line in lines)
                {
                    var columns = Regex.Split(line, @"\s{2,}|\t|\|")
                        .Select(x => x.Trim())
                        .ToList();
                    matrix.Add(columns);
                }
            }

            return matrix;
        }
        private static async Task<List<List<string>>> ReadImageMatrixWithOcrAsync(IFormFile file)
        {
            var matrix = new List<List<string>>();

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            try
            {
                var tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tessdata");

                using var engine = new Tesseract.TesseractEngine(tessDataPath, "eng", Tesseract.EngineMode.Default);
                using var img = Tesseract.Pix.LoadFromMemory(memoryStream.ToArray());

                using Tesseract.Page ocrPage = engine.Process(img);

                var extractedText = ocrPage.GetText();

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new InvalidOperationException("Could not read any text from the image. Ensure the image is clear and well-lit.");
                }

                var lines = extractedText
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                foreach (var line in lines)
                {
                    var cells = SplitPdfLine(line);
                    if (cells.Count > 0)
                    {
                        matrix.Add(cells);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error during OCR processing: {ex.Message}");
            }

            return matrix;
        }

        private static List<string> SplitPdfLine(string line)
        {
            if (line.Contains('|'))
            {
                return line.Split('|').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            if (line.Contains('\t'))
            {
                return line.Split('\t').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            return System.Text.RegularExpressions.Regex
                .Split(line, @"\s{2,}")
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }


    }



}
