using System.Data;

namespace EasyBill.UI.Service.ExcelService
{
    public interface IExcelService
    {
        byte[] ExportToExcel<T>(IEnumerable<T> data, Dictionary<string, Func<T, object?>> columnMap, string sheetName);
        Task<List<T>> ImportAsync<T>(byte[] fileBytes, Dictionary<string, Func<DataRow, T, object?>> mappers, string sheetName = "Sheet1") where T : new();
    }
}
