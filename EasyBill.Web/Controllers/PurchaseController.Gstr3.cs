using AOne.Utility.Enums;
using ClosedXML.Excel;
using EasyBill.Models.Entity;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace EasyBill.UI.Controllers
{
    public partial class PurchaseController
    {
        private sealed class Gstr3TaxAccumulator
        {
            public decimal TaxableValue { get; private set; }
            public decimal IntegratedTax { get; private set; }
            public decimal CentralTax { get; private set; }
            public decimal StateUtTax { get; private set; }
            public decimal Cess { get; private set; }

            public void Add(decimal taxableValue, decimal integratedTax, decimal centralTax, decimal stateUtTax, decimal cess)
            {
                TaxableValue += taxableValue;
                IntegratedTax += integratedTax;
                CentralTax += centralTax;
                StateUtTax += stateUtTax;
                Cess += cess;
            }
        }

        private async Task<Gstr3ReportVM> BuildGstr3ReportAsync(DateTime? startDate, DateTime? endDate)
        {
            var resolvedStart = (startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var resolvedEnd = (endDate ?? DateTime.Today).Date;

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _tenantService.GetById(user?.TenantId);
            var tenantGstin = (tenant?.GstNo ?? string.Empty).Trim();
            var tenantStateCode = GetStateCodeFromGstin(tenantGstin);

            var sales = await _salesService.GetAll();
            var purchases = await _purchaseservice.GetAll();
            var suppliers = await _supplierservice.GetALL();
            var itemMasters = await _itemmasterservice.GetAll();

            var supplierLookup = suppliers.ToDictionary(x => x.Id);
            var itemLookup = itemMasters
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var outwardTaxable = new Gstr3TaxAccumulator();
            var outwardZeroRated = new Gstr3TaxAccumulator();
            var outwardNilExempt = new Gstr3TaxAccumulator();
            var inwardReverseCharge = new Gstr3TaxAccumulator();
            var outwardNonGst = new Gstr3TaxAccumulator();

            var section32Accumulator = new Dictionary<string, Gstr3TaxAccumulator>(StringComparer.OrdinalIgnoreCase);

            foreach (var sale in sales.Where(x => x.BillDate.HasValue).Where(x => x.BillDate!.Value.Date >= resolvedStart && x.BillDate.Value.Date <= resolvedEnd))
            {
                var validItems = (sale.SalesItems ?? new List<SalesItem>()).Where(x => x.Deleted == null && x.ItemMaster != null).ToList();

                if (!validItems.Any())
                {
                    continue;
                }

                var customerGstin = (sale.Customers?.GSTNo ?? string.Empty).Trim();
                var customerStateCode = GetStateCodeFromGstin(customerGstin);
                var isInterState = IsInterStateSupply(customerStateCode, tenantStateCode, sale.billingType);

                var taxable = sale.Total;
                var totalGst = sale.TotalGstAmt;
                var cess = sale.TotalCessAmount;

                var igst = 0m;
                var cgst = 0m;
                var sgst = 0m;

                if (totalGst > 0)
                {
                    if (isInterState)
                    {
                        igst = totalGst;
                    }
                    else
                    {
                        cgst = totalGst / 2m;
                        sgst = totalGst - cgst;
                    }
                }

                var isNilRated = validItems.All(i =>
                    i.Gst <= 0 &&
                    i.ItemMaster!.Local == TaxStatus.Taxable &&
                    i.ItemMaster.Central == TaxStatus.Taxable);

                var isExempted = validItems.All(i =>
                    i.ItemMaster!.Local == TaxStatus.Exempted &&
                    i.ItemMaster.Central == TaxStatus.Exempted);

                var isNonGst = validItems.All(i =>
                    i.ItemMaster!.Local == TaxStatus.TaxPaid &&
                    i.ItemMaster.Central == TaxStatus.TaxPaid);

                var isZeroRated = !string.IsNullOrWhiteSpace(sale.billingType) &&
                                  sale.billingType.Contains("export", StringComparison.OrdinalIgnoreCase);

                var taxableOutwardSupply = !isZeroRated && !isNonGst && !isNilRated && !isExempted;

                if (isZeroRated)
                {
                    outwardZeroRated.Add(taxable, igst, cgst, sgst, cess);
                }
                else if (isNonGst)
                {
                    outwardNonGst.Add(taxable, igst, cgst, sgst, cess);
                }
                else if (!taxableOutwardSupply)
                {
                    outwardNilExempt.Add(taxable, igst, cgst, sgst, cess);
                }
                else
                {
                    outwardTaxable.Add(taxable, igst, cgst, sgst, cess);

                    if (isInterState && string.IsNullOrWhiteSpace(customerGstin))
                    {
                        const string placeOfSupply = "Other Territory";
                        if (!section32Accumulator.TryGetValue(placeOfSupply, out var aggregate))
                        {
                            aggregate = new Gstr3TaxAccumulator();
                            section32Accumulator[placeOfSupply] = aggregate;
                        }

                        aggregate.Add(taxable, igst, 0, 0, 0);
                    }
                }
            }

            var itcReverseCharge = new Gstr3TaxAccumulator();
            var itcAllOther = new Gstr3TaxAccumulator();

            decimal section5NilExemptInter = 0m;
            decimal section5NilExemptIntra = 0m;
            decimal section5NonGstInter = 0m;
            decimal section5NonGstIntra = 0m;
            foreach (var purchase in purchases
                         .Where(x => x.BillDate.HasValue)
                         .Where(x => x.BillDate!.Value.Date >= resolvedStart && x.BillDate.Value.Date <= resolvedEnd))
            {
                var validItems = (purchase.PurchaseItems ?? new List<PurchaseItem>())
                    .Where(x => x.Deleted == null && (x.Qty + x.FreeQty) > 0)
                    .ToList();

                if (!validItems.Any())
                {
                    continue;
                }

                Supplier? supplier = null;
                if (purchase.SupplierId.HasValue)
                {
                    supplierLookup.TryGetValue(purchase.SupplierId.Value, out supplier);
                }

                var supplierGstin = (supplier?.GstNO ?? string.Empty).Trim();
                var supplierStateCode = GetStateCodeFromGstin(supplierGstin);
                var isInterState = IsInterStateSupply(supplierStateCode, tenantStateCode, purchase.PurchaseType);

                var taxable = purchase.Total;
                var totalGst = purchase.TotalGstAmt;
                var cgst = purchase.TotalCGstAmt;
                var sgst = purchase.TotalSGstAmt;
                var cess = purchase.TotalCessAmt;

                var igst = totalGst - (cgst + sgst);
                if (igst < 0)
                {
                    igst = 0;
                }

                if (totalGst > 0 && (cgst + sgst) <= 0)
                {
                    if (isInterState)
                    {
                        igst = totalGst;
                    }
                    else
                    {
                        cgst = totalGst / 2m;
                        sgst = totalGst - cgst;
                        igst = 0;
                    }
                }

                var hasTax = (igst + cgst + sgst + cess) > 0;
                var isReverseCharge = string.IsNullOrWhiteSpace(supplierGstin) && hasTax;

                if (isReverseCharge)
                {
                    inwardReverseCharge.Add(taxable, igst, cgst, sgst, cess);
                    itcReverseCharge.Add(0, igst, cgst, sgst, cess);
                }
                else if (hasTax)
                {
                    itcAllOther.Add(0, igst, cgst, sgst, cess);
                }

                foreach (var item in validItems)
                {
                    if (!itemLookup.TryGetValue(item.ItemId, out var itemMaster))
                    {
                        continue;
                    }

                    var taxableValue = CalculatePurchaseItemTaxable(purchase, item);
                    if (taxableValue <= 0)
                    {
                        continue;
                    }

                    var isNonGstSupply = itemMaster.Local == TaxStatus.TaxPaid &&
                                         itemMaster.Central == TaxStatus.TaxPaid;

                    var isNilOrExemptSupply =
                        (item.Gst <= 0 &&
                         itemMaster.Local == TaxStatus.Taxable &&
                         itemMaster.Central == TaxStatus.Taxable) ||
                        (itemMaster.Local == TaxStatus.Exempted &&
                         itemMaster.Central == TaxStatus.Exempted);

                    if (isNonGstSupply)
                    {
                        if (isInterState)
                        {
                            section5NonGstInter += taxableValue;
                        }
                        else
                        {
                            section5NonGstIntra += taxableValue;
                        }
                    }
                    else if (isNilOrExemptSupply)
                    {
                        if (isInterState)
                        {
                            section5NilExemptInter += taxableValue;
                        }
                        else
                        {
                            section5NilExemptIntra += taxableValue;
                        }
                    }
                }
            }

            var section31Rows = new List<Gstr3Section31RowVM>
            {
                CreateSection31Row("(a)", "Outward taxable supplies (other than zero rated, nil rated and exempted)", outwardTaxable),
                CreateSection31Row("(b)", "Outward taxable supplies (zero rated)", outwardZeroRated),
                CreateSection31Row("(c)", "Other outward supplies (nil rated, exempted)", outwardNilExempt),
                CreateSection31Row("(d)", "Inward supplies (liable to reverse charge)", inwardReverseCharge),
                CreateSection31Row("(e)", "Non-GST outward supplies", outwardNonGst)
            };

            var section32Rows = section32Accumulator
                .Select(x => new Gstr3Section32RowVM
                {
                    PlaceOfSupply = x.Key,
                    TotalTaxableValue = Round2(x.Value.TaxableValue),
                    AmountOfIntegratedTax = Round2(x.Value.IntegratedTax)
                })
                .OrderBy(x => x.PlaceOfSupply)
                .ToList();

            if (!section32Rows.Any())
            {
                section32Rows.Add(new Gstr3Section32RowVM
                {
                    PlaceOfSupply = "-",
                    TotalTaxableValue = 0,
                    AmountOfIntegratedTax = 0
                });
            }

            var itcAvailableA = new Gstr3TaxAccumulator();
            itcAvailableA.Add(
                0,
                itcReverseCharge.IntegratedTax + itcAllOther.IntegratedTax,
                itcReverseCharge.CentralTax + itcAllOther.CentralTax,
                itcReverseCharge.StateUtTax + itcAllOther.StateUtTax,
                itcReverseCharge.Cess + itcAllOther.Cess);

            var itcReversedB = new Gstr3TaxAccumulator();

            var netItc = new Gstr3TaxAccumulator();
            netItc.Add(
                0,
                itcAvailableA.IntegratedTax - itcReversedB.IntegratedTax,
                itcAvailableA.CentralTax - itcReversedB.CentralTax,
                itcAvailableA.StateUtTax - itcReversedB.StateUtTax,
                itcAvailableA.Cess - itcReversedB.Cess);

            var section4Rows = new List<Gstr3Section4RowVM>
            {
                new() { Code = "(A)", Details = "ITC available (whether in full or part)", IsHeader = true },
                new() { Code = "(1)", Details = "Import of Goods" },
                new() { Code = "(2)", Details = "Import of Services" },
                new()
                {
                    Code = "(3)",
                    Details = "Inward supplies liable to reverse charge (other than 1 & 2 above)",
                    IntegratedTax = Round2(itcReverseCharge.IntegratedTax),
                    CentralTax = Round2(itcReverseCharge.CentralTax),
                    StateUtTax = Round2(itcReverseCharge.StateUtTax),
                    Cess = Round2(itcReverseCharge.Cess)
                },
                new() { Code = "(4)", Details = "Inward supplies from ISD" },
                new()
                {
                    Code = "(5)",
                    Details = "All other ITC",
                    IntegratedTax = Round2(itcAllOther.IntegratedTax),
                    CentralTax = Round2(itcAllOther.CentralTax),
                    StateUtTax = Round2(itcAllOther.StateUtTax),
                    Cess = Round2(itcAllOther.Cess)
                },
                new() { Code = "(B)", Details = "ITC Reversed", IsHeader = true },
                new() { Code = "(1)", Details = "As per rules 38,42 & 43 of CGST rules and section 17(5)" },
                new() { Code = "(2)", Details = "Others" },
                new()
                {
                    Code = "(C)",
                    Details = "Net ITC available (A)-(B)",
                    IntegratedTax = Round2(netItc.IntegratedTax),
                    CentralTax = Round2(netItc.CentralTax),
                    StateUtTax = Round2(netItc.StateUtTax),
                    Cess = Round2(netItc.Cess),
                    IsHeader = true
                },
                new() { Code = "(D)", Details = "Other Details", IsHeader = true },
                new() { Code = "(1)", Details = "ITC reclaimed which was reversed under Table 4(B)(2) in earlier tax period" },
                new() { Code = "(2)", Details = "Ineligible ITC under section 16(4) & ITC restricted due to POS rules" }
            };

            var section5Rows = new List<Gstr3Section5RowVM>
            {
                new()
                {
                    NatureOfSupplies = "From a supplier under composition scheme, Exempt and Nil rated supply",
                    InterStateSupplies = Round2(section5NilExemptInter),
                    IntraStateSupplies = Round2(section5NilExemptIntra)
                },
                new()
                {
                    NatureOfSupplies = "Non GST supply",
                    InterStateSupplies = Round2(section5NonGstInter),
                    IntraStateSupplies = Round2(section5NonGstIntra)
                }
            };

            var taxPayableIntegrated = section31Rows.Sum(x => x.IntegratedTax);
            var taxPayableCentral = section31Rows.Sum(x => x.CentralTax);
            var taxPayableState = section31Rows.Sum(x => x.StateUtTax);
            var taxPayableCess = section31Rows.Sum(x => x.Cess);

            var paidItcIntegrated = Math.Min(taxPayableIntegrated, netItc.IntegratedTax);
            var paidItcCentral = Math.Min(taxPayableCentral, netItc.CentralTax);
            var paidItcState = Math.Min(taxPayableState, netItc.StateUtTax);
            var paidItcCess = Math.Min(taxPayableCess, netItc.Cess);

            var section61Rows = new List<Gstr3Section61RowVM>
            {
                new()
                {
                    Description = "Integrated Tax",
                    TaxPayable = Round2(taxPayableIntegrated),
                    PaidThroughItcIntegratedTax = Round2(paidItcIntegrated),
                    TaxPaidCash = Round2(taxPayableIntegrated - paidItcIntegrated)
                },
                new()
                {
                    Description = "Central Tax",
                    TaxPayable = Round2(taxPayableCentral),
                    PaidThroughItcCentralTax = Round2(paidItcCentral),
                    TaxPaidCash = Round2(taxPayableCentral - paidItcCentral)
                },
                new()
                {
                    Description = "State/UT Tax",
                    TaxPayable = Round2(taxPayableState),
                    PaidThroughItcStateUtTax = Round2(paidItcState),
                    TaxPaidCash = Round2(taxPayableState - paidItcState)
                },
                new()
                {
                    Description = "Cess",
                    TaxPayable = Round2(taxPayableCess),
                    PaidThroughItcCess = Round2(paidItcCess),
                    TaxPaidCash = Round2(taxPayableCess - paidItcCess)
                }
            };

            var section62Rows = new List<Gstr3Section62RowVM>
            {
                new() { Details = "TDS" },
                new() { Details = "TCS" }
            };

            return new Gstr3ReportVM
            {
                StartBillDate = resolvedStart,
                EndBillDate = resolvedEnd,
                GSTIN = tenantGstin,
                LegalName = tenant?.Name ?? string.Empty,
                Year = resolvedStart.Year,
                Month = resolvedStart.ToString("MMMM", CultureInfo.InvariantCulture),
                Section31Rows = section31Rows,
                Section32Rows = section32Rows,
                Section4Rows = section4Rows,
                Section5Rows = section5Rows,
                Section61Rows = section61Rows,
                Section62Rows = section62Rows
            };
        }
        private async Task<IActionResult> ExportGstr3ExcelInternal(DateTime? startBillDate, DateTime? endBillDate)
        {
            if (!startBillDate.HasValue)
            {
                startBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!endBillDate.HasValue)
            {
                endBillDate = DateTime.Today;
            }

            var report = await BuildGstr3ReportAsync(startBillDate, endBillDate);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("GSTR3B Report");
            var row = 1;

            ws.Cell(row, 1).Value = "Form GSTR-3B";
            ws.Range(row, 1, row, 10).Merge();
            ws.Range(row, 1, row, 10).Style.Font.SetBold().Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            row++;
            ws.Cell(row, 1).Value = $"GSTIN : {report.GSTIN}";
            ws.Cell(row, 4).Value = $"Legal Name : {report.LegalName}";
            ws.Cell(row, 8).Value = $"Year : {report.Year}";
            ws.Cell(row, 9).Value = $"Month : {report.Month}";
            row += 2;

            ws.Cell(row, 1).Value = "3.1 Details of Outward Supplies and Inward supplies liable to reverse charges";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;

            ws.Cell(row, 1).Value = "Name of Supplies";
            ws.Cell(row, 2).Value = "Total Taxable Value";
            ws.Cell(row, 3).Value = "Integrated Tax";
            ws.Cell(row, 4).Value = "Central Tax";
            ws.Cell(row, 5).Value = "State/UT Tax";
            ws.Cell(row, 6).Value = "Cess";
            ws.Range(row, 1, row, 6).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section31Rows)
            {
                ws.Cell(row, 1).Value = $"{item.Code} {item.Description}";
                ws.Cell(row, 2).Value = item.TaxableValue;
                ws.Cell(row, 3).Value = item.IntegratedTax;
                ws.Cell(row, 4).Value = item.CentralTax;
                ws.Cell(row, 5).Value = item.StateUtTax;
                ws.Cell(row, 6).Value = item.Cess;
                row++;
            }

            row++;
            ws.Cell(row, 1).Value = "3.2 Of the supplies shown in 3.1(a) above, details of inter-state supplies made to unregistered persons, composition taxable persons and UIN holders";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;
            ws.Cell(row, 1).Value = "Place of Supply (State/UT)";
            ws.Cell(row, 2).Value = "Total Taxable Value";
            ws.Cell(row, 3).Value = "Amount of Integrated Tax";
            ws.Range(row, 1, row, 3).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section32Rows)
            {
                ws.Cell(row, 1).Value = item.PlaceOfSupply;
                ws.Cell(row, 2).Value = item.TotalTaxableValue;
                ws.Cell(row, 3).Value = item.AmountOfIntegratedTax;
                row++;
            }

            row++;
            ws.Cell(row, 1).Value = "4. Eligible ITC";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;
            ws.Cell(row, 1).Value = "Details";
            ws.Cell(row, 2).Value = "Integrated Tax";
            ws.Cell(row, 3).Value = "Central Tax";
            ws.Cell(row, 4).Value = "State/UT Tax";
            ws.Cell(row, 5).Value = "Cess";
            ws.Range(row, 1, row, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section4Rows)
            {
                ws.Cell(row, 1).Value = $"{item.Code} {item.Details}";
                ws.Cell(row, 2).Value = item.IntegratedTax;
                ws.Cell(row, 3).Value = item.CentralTax;
                ws.Cell(row, 4).Value = item.StateUtTax;
                ws.Cell(row, 5).Value = item.Cess;

                if (item.IsHeader)
                {
                    ws.Range(row, 1, row, 5).Style.Font.SetBold();
                    ws.Range(row, 1, row, 5).Style.Fill.SetBackgroundColor(XLColor.AliceBlue);
                }

                row++;
            }

            row++;
            ws.Cell(row, 1).Value = "5. Values of Exempt, Nil-rated and Non-GST inward supplies";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;
            ws.Cell(row, 1).Value = "Nature of Supplies";
            ws.Cell(row, 2).Value = "Inter-State Supplies";
            ws.Cell(row, 3).Value = "Intra-State Supplies";
            ws.Range(row, 1, row, 3).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section5Rows)
            {
                ws.Cell(row, 1).Value = item.NatureOfSupplies;
                ws.Cell(row, 2).Value = item.InterStateSupplies;
                ws.Cell(row, 3).Value = item.IntraStateSupplies;
                row++;
            }

            row++;
            ws.Cell(row, 1).Value = "6.1 Payment of Tax";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;
            ws.Cell(row, 1).Value = "Description";
            ws.Cell(row, 2).Value = "Tax Payable";
            ws.Cell(row, 3).Value = "Paid Through ITC - Integrated Tax";
            ws.Cell(row, 4).Value = "Paid Through ITC - Central Tax";
            ws.Cell(row, 5).Value = "Paid Through ITC - State/UT Tax";
            ws.Cell(row, 6).Value = "Paid Through ITC - Cess";
            ws.Cell(row, 7).Value = "Tax Paid TDS/TCS";
            ws.Cell(row, 8).Value = "Tax Paid in Cash";
            ws.Cell(row, 9).Value = "Interest";
            ws.Cell(row, 10).Value = "Late Fee";
            ws.Range(row, 1, row, 10).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section61Rows)
            {
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.TaxPayable;
                ws.Cell(row, 3).Value = item.PaidThroughItcIntegratedTax;
                ws.Cell(row, 4).Value = item.PaidThroughItcCentralTax;
                ws.Cell(row, 5).Value = item.PaidThroughItcStateUtTax;
                ws.Cell(row, 6).Value = item.PaidThroughItcCess;
                ws.Cell(row, 7).Value = item.TaxPaidTdsTcs;
                ws.Cell(row, 8).Value = item.TaxPaidCash;
                ws.Cell(row, 9).Value = item.Interest;
                ws.Cell(row, 10).Value = item.LateFee;
                row++;
            }

            row++;
            ws.Cell(row, 1).Value = "6.2 TDS/TCS Credit";
            ws.Range(row, 1, row, 10).Merge().Style.Font.SetBold();
            row++;
            ws.Cell(row, 1).Value = "Details";
            ws.Cell(row, 2).Value = "Integrated Tax";
            ws.Cell(row, 3).Value = "Central Tax";
            ws.Cell(row, 4).Value = "State/UT Tax";
            ws.Range(row, 1, row, 4).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var item in report.Section62Rows)
            {
                ws.Cell(row, 1).Value = item.Details;
                ws.Cell(row, 2).Value = item.IntegratedTax;
                ws.Cell(row, 3).Value = item.CentralTax;
                ws.Cell(row, 4).Value = item.StateUtTax;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"GSTR3B_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }
        private async Task<IActionResult> ExportGstr3CsvInternal(DateTime? startBillDate, DateTime? endBillDate)
        {
            if (!startBillDate.HasValue)
            {
                startBillDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }

            if (!endBillDate.HasValue)
            {
                endBillDate = DateTime.Today;
            }

            var report = await BuildGstr3ReportAsync(startBillDate, endBillDate);

            var sb = new StringBuilder();
            sb.AppendLine("Form GSTR-3B");
            sb.AppendLine($"GSTIN,{EscapeCsvValue(report.GSTIN)}");
            sb.AppendLine($"Legal Name,{EscapeCsvValue(report.LegalName)}");
            sb.AppendLine($"Year,{report.Year}");
            sb.AppendLine($"Month,{EscapeCsvValue(report.Month)}");
            sb.AppendLine();

            sb.AppendLine("3.1 Details of Outward Supplies and Inward supplies liable to reverse charges");
            sb.AppendLine("Code,Name of Supplies,Total Taxable Value,Integrated Tax,Central Tax,State/UT Tax,Cess");
            foreach (var item in report.Section31Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.Code)}," +
                    $"{EscapeCsvValue(item.Description)}," +
                    $"{item.TaxableValue:F2},{item.IntegratedTax:F2},{item.CentralTax:F2},{item.StateUtTax:F2},{item.Cess:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("3.2 Of the supplies shown in 3.1(a), inter-state supplies");
            sb.AppendLine("Place of Supply (State/UT),Total Taxable Value,Amount of Integrated Tax");
            foreach (var item in report.Section32Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.PlaceOfSupply)}," +
                    $"{item.TotalTaxableValue:F2},{item.AmountOfIntegratedTax:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("4. Eligible ITC");
            sb.AppendLine("Code,Details,Integrated Tax,Central Tax,State/UT Tax,Cess");
            foreach (var item in report.Section4Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.Code)}," +
                    $"{EscapeCsvValue(item.Details)}," +
                    $"{item.IntegratedTax:F2},{item.CentralTax:F2},{item.StateUtTax:F2},{item.Cess:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("5. Values of Exempt, Nil-rated and Non-GST inward supplies");
            sb.AppendLine("Nature of Supplies,Inter-State Supplies,Intra-State Supplies");
            foreach (var item in report.Section5Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.NatureOfSupplies)}," +
                    $"{item.InterStateSupplies:F2},{item.IntraStateSupplies:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("6.1 Payment of Tax");
            sb.AppendLine("Description,Tax Payable,Paid Through ITC - Integrated,Paid Through ITC - Central,Paid Through ITC - State/UT,Paid Through ITC - Cess,Tax Paid TDS/TCS,Tax Paid in Cash,Interest,Late Fee");
            foreach (var item in report.Section61Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.Description)}," +
                    $"{item.TaxPayable:F2}," +
                    $"{item.PaidThroughItcIntegratedTax:F2}," +
                    $"{item.PaidThroughItcCentralTax:F2}," +
                    $"{item.PaidThroughItcStateUtTax:F2}," +
                    $"{item.PaidThroughItcCess:F2}," +
                    $"{item.TaxPaidTdsTcs:F2}," +
                    $"{item.TaxPaidCash:F2}," +
                    $"{item.Interest:F2}," +
                    $"{item.LateFee:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("6.2 TDS/TCS Credit");
            sb.AppendLine("Details,Integrated Tax,Central Tax,State/UT Tax");
            foreach (var item in report.Section62Rows)
            {
                sb.AppendLine(
                    $"{EscapeCsvValue(item.Details)}," +
                    $"{item.IntegratedTax:F2},{item.CentralTax:F2},{item.StateUtTax:F2}");
            }

            return File(
                Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                $"GSTR3B_Report_{DateTime.Now:yyyyMMdd}.csv");
        }

        private static Gstr3Section31RowVM CreateSection31Row(string code, string description, Gstr3TaxAccumulator source)
        {
            return new Gstr3Section31RowVM
            {
                Code = code,
                Description = description,
                TaxableValue = Round2(source.TaxableValue),
                IntegratedTax = Round2(source.IntegratedTax),
                CentralTax = Round2(source.CentralTax),
                StateUtTax = Round2(source.StateUtTax),
                Cess = Round2(source.Cess)
            };
        }

        private static decimal CalculatePurchaseItemTaxable(Purchase purchase, PurchaseItem item)
        {
            var baseAmount = item.Qty * item.Rate;
            var itemDiscountAmount = baseAmount * item.Discount / 100m;
            var afterItemDiscount = baseAmount - itemDiscountAmount;
            var billDiscountAmount = afterItemDiscount * purchase.discountPercent / 100m;
            var taxable = afterItemDiscount - billDiscountAmount;

            if (taxable < 0)
            {
                taxable = 0;
            }

            return taxable;
        }

        private static string? GetStateCodeFromGstin(string? gstin)
        {
            if (string.IsNullOrWhiteSpace(gstin))
            {
                return null;
            }

            var clean = gstin.Trim();
            if (clean.Length < 2)
            {
                return null;
            }

            var stateCode = clean.Substring(0, 2);
            return stateCode.All(char.IsDigit) ? stateCode : null;
        }

        private static bool IsInterStateSupply(string? counterPartyStateCode, string? tenantStateCode, string? billType)
        {
            if (!string.IsNullOrWhiteSpace(billType))
            {
                if (billType.Equals("Central", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (billType.Equals("Local", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(counterPartyStateCode) || string.IsNullOrWhiteSpace(tenantStateCode))
            {
                return false;
            }

            return !counterPartyStateCode.Equals(tenantStateCode, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal Round2(decimal value)
        {
            return Math.Round(value, 2);
        }
    }
}
