using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OfficeOpenXml;
using System.Data;

namespace EasyBill.UI.Service.ExcelService
{
    public class ExcelServices : IExcelService
    {
        public byte[] ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, Func<T, object?>> columnMap, string sheetName)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            int colIndex = 1;

            // Add Headers
            foreach (var header in columnMap.Keys)
            {
                worksheet.Cells[1, colIndex].Value = header;
                worksheet.Cells[1, colIndex].Style.Font.Bold = true;
                colIndex++;
            }

            int rowIndex = 2;
            foreach (var item in data)
            {
                colIndex = 1;
                foreach (var selector in columnMap.Values)
                {
                    worksheet.Cells[rowIndex, colIndex].Value = selector(item);
                    colIndex++;
                }
                rowIndex++;
            }

            worksheet.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }
        public async Task<List<T>> ImportAsync<T>(
    byte[] fileBytes,
    Dictionary<string, Func<DataRow, T, object?>> mappers,
    string sheetName = ""
) where T : new()
        {
            var result = new List<T>();

            using var stream = new MemoryStream(fileBytes);
            IWorkbook workbook;

            // ✅ Auto detect xls / xlsx
            try
            {
                workbook = WorkbookFactory.Create(stream);
            }
            catch
            {
                throw new Exception("Invalid Excel file. Sirf .xls ya .xlsx supported hai.");
            }

            if (workbook.NumberOfSheets == 0)
                throw new Exception("Excel me koi sheet nahi hai.");

            ISheet sheet = !string.IsNullOrWhiteSpace(sheetName)
                ? workbook.GetSheet(sheetName) ?? workbook.GetSheetAt(0)
                : workbook.GetSheetAt(0);

            if (sheet.PhysicalNumberOfRows <= 1)
                throw new Exception("Excel sheet blank hai.");

            var headerRow = sheet.GetRow(0);
            if (headerRow == null)
                throw new Exception("Excel ka header row missing hai.");

            var dataTable = new DataTable();

            // ✅ Build columns safely
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var colName = headerRow.GetCell(i)?.ToString()?.Trim();

                if (string.IsNullOrEmpty(colName))
                    colName = $"Column{i}";

                if (!dataTable.Columns.Contains(colName))
                    dataTable.Columns.Add(colName);
            }

            // ✅ Read rows (IndexOutOfRange safe)
            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                var dataRow = dataTable.NewRow();

                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    var cell = row.GetCell(j);
                    dataRow[j] = cell == null ? null : cell.ToString();
                }

                dataTable.Rows.Add(dataRow);
            }

            // ✅ Map to model (missing columns silently ignored)
            foreach (DataRow dr in dataTable.Rows)
            {
                var item = new T();

                foreach (var map in mappers)
                {
                    if (dr.Table.Columns
                        .Cast<DataColumn>()
                        .Any(c => c.ColumnName.Equals(map.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        map.Value(dr, item);
                    }
                }

                result.Add(item);
            }

            return await Task.FromResult(result);
        }


        // public async Task<List<T>> ImportAsync<T>(byte[] fileBytes, Dictionary<string, Func<DataRow, T, object?>> mappers, string sheetName = "Sheet1" ) where T : new()
        // {
        //     ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        //     var result = new List<T>();
        //     using var stream = new MemoryStream(fileBytes); 
        //     using var package = new ExcelPackage(stream); 
        //     // var worksheet = package.Workbook.Worksheets[sheetName] ?? package.Workbook.Worksheets.First();
        //     if (package.Workbook.Worksheets.Count == 0)
        //     {
        //         throw new Exception("Invalid Excel file. Sirf .xlsx supported hai.");
        //     }

        //     var worksheet = package.Workbook.Worksheets
        //.FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
        //?? package.Workbook.Worksheets.First();

        //     // ✅ SAFETY CHECK 3: empty sheet
        //     if (worksheet.Dimension == null)
        //     {
        //         throw new Exception("Excel sheet blank hai (no data found).");
        //     }
        //     var dataTable = new DataTable();
        //     bool hasHeader = true;
        //     foreach (var firstRowCell in worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column])
        //     {
        //         dataTable.Columns.Add(hasHeader ? firstRowCell.Text : $"Column {firstRowCell.Start.Column}");
        //     }

        //     var startRow = hasHeader ? 2 : 1;
        //     for (int rowNum = startRow; rowNum <= worksheet.Dimension.End.Row; rowNum++)
        //     {
        //         var row = worksheet.Cells[rowNum, 1, rowNum, worksheet.Dimension.End.Column];
        //         var newRow = dataTable.NewRow();
        //         for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        //         {
        //             newRow[col - 1] = row[rowNum, col].Text;
        //         }
        //         dataTable.Rows.Add(newRow);
        //     }

        //     foreach (DataRow dr in dataTable.Rows)
        //     {
        //         var item = new T();
        //         foreach (var map in mappers)
        //         {
        //             map.Value(dr, item);
        //         }
        //         result.Add(item);
        //     }

        //     return await Task.FromResult(result);
        // }
    }
}
