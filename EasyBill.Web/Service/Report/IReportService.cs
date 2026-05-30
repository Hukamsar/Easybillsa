namespace AOneWeb.Service.Report;

public interface IReportService
{
    byte[] GenerateReportAsync(string reportName, string reportType, string dsName = null, object reportList = null);
    byte[] GenerateReportAsync(string reportName, string reportType, string dsName = null, object reportList = null, Dictionary<string, string> Parameters = null);
    Task<byte[]> GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null,string dsName3 = null,string dsName4 = null, object reportList1 = null, object reportList2 = null, object reportList3 = null,object reportList4 = null, Dictionary<string, string> parameters = null);

    byte[] GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null,  object reportList1 = null, object reportList2 = null,  Dictionary<string, string> parameters = null);
    byte[] GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, string dsName3 = null,  object reportList1 = null, object reportList2 = null, object reportList3 = null,  Dictionary<string, string> parameters = null);

    byte[] GenerateReportSubReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, string dsName3 = null, string dsName4 = null, object reportList1 = null, object reportList2 = null, object reportList3 = null, object reportList4 = null, Dictionary<string, string> parameters = null);


}

