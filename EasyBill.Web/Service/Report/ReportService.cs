

using System.Reflection;
using System.Text;
using Microsoft.Reporting.NETCore;

namespace AOneWeb.Service.Report;

public enum ReportTypeParams
{
    word,
    excel,
    pdf,
    html
}

public enum ReportTypeExtensions
{
    docx,
    xlsx,
    pdf,
    html
}
public class ReportService : IReportService
{
    public const string defaultReportType = "pdf";

    public static string GetReportTypeExtension(string reportType)
    {
        if (reportType.Equals(nameof(ReportTypeExtensions.docx), StringComparison.InvariantCultureIgnoreCase))
        {
            return nameof(ReportTypeExtensions.docx);
        }
        else if (reportType.Equals(nameof(ReportTypeExtensions.xlsx), StringComparison.InvariantCultureIgnoreCase))
        {
            return nameof(ReportTypeExtensions.xlsx);
        }
        else if (reportType.Equals(nameof(ReportTypeExtensions.html), StringComparison.InvariantCultureIgnoreCase))
        {
            return nameof(ReportTypeExtensions.html);
        }

        return nameof(ReportTypeExtensions.pdf);
    }

    public byte[] GenerateReportAsync(string reportName, string reportType, string dsName = null, object reportList = null)
    {
        string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
        string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
        var parameters = new Dictionary<string, string>();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.GetEncoding("windows-1252");
        string _reportFormat = "pdf";
        switch (reportType)
        {
            case "xlsx":
                _reportFormat = "EXCELOPENXML";
                break;
            case "docx":
                _reportFormat = "WORDOPENXML";
                break;
            case "html":
                _reportFormat = "HTML5";
                break;
            case "pdf":
                _reportFormat = "PDF";
                break;
        }
        using var fs = new FileStream(rdlcFilePath, FileMode.Open);
        var report = new LocalReport();

        report.LoadReportDefinition(fs);
       
        if (dsName != null && reportList != null)
        {
            report.DataSources.Add( new ReportDataSource(dsName, reportList));
        }
        //if(parameters.Count > 0)
        //    report.SetParameters(new[] { new ReportParameter("Date", DateTime.Now.Date.ToString()) });
        var result = report.Render(_reportFormat);
        return result;
    }
   

    public byte[] GenerateReportAsync(string reportName, string reportType, string dsName = null, object reportList = null, Dictionary<string, string> parameters = null)
    {
        try
        {
            //string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
            //string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
            string fileDirPath = AppDomain.CurrentDomain.BaseDirectory;
            string rdlcFilePath = Path.Combine(fileDirPath, "Reports", $"{reportName}.rdlc");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding.GetEncoding("windows-1252");
            string _reportFormat = "pdf";
            switch(reportType)
            {
                case "xlsx":
                    _reportFormat = "EXCELOPENXML";
                    break;
                case "docx":
                    _reportFormat = "WORDOPENXML";
                    break;
                case "html":
                    _reportFormat = "HTML5";
                    break;
                case "pdf":
                    _reportFormat = "PDF";
                    break;

            }
            if (parameters == null)
                parameters = new Dictionary<string, string>();

            using var fs = new FileStream(rdlcFilePath, FileMode.Open);

            var report = new LocalReport();
            report.EnableExternalImages = true;
            report.LoadReportDefinition(fs);
            if (dsName != null && reportList != null)
            {
                if (reportList is IEnumerable<object> enumerableList)
                {
                    report.DataSources.Add(new ReportDataSource(dsName, enumerableList));
                }
                else if (reportList is System.Data.DataTable dataTable)
                {
                    report.DataSources.Add(new ReportDataSource(dsName, dataTable));
                }
               // report.DataSources.Add(new ReportDataSource(dsName, reportList));
            }
            if (parameters != null)
            {
                foreach(var parm in parameters)
                {
                    report.SetParameters(new[] { new ReportParameter(parm.Key, parm.Value.ToString()) });
                }
                
            }
            var result = report.Render(_reportFormat);
            return result;
        }
        catch(Exception ex)
        {
            throw ex;
        }
            
      
    }

    public async Task<byte[]> GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, string dsName3 = null, string dsName4 = null, object reportList1 = null, object reportList2 = null, object reportList3 = null, object reportList4 = null, Dictionary<string, string> parameters = null)
    {
        string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
        string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.GetEncoding("windows-1252");
        string _reportFormat = "pdf";
        switch(reportType)
        {
            case "xlsx":
                _reportFormat = "EXCELOPENXML";
                break;
            case "docx":
                _reportFormat = "WORDOPENXML";
                break;
            case "html":
                _reportFormat = "HTML5";
                break;
            case "pdf":
                _reportFormat = "PDF";
                break;
        }
        if (parameters == null)
            parameters = new Dictionary<string, string>();
        using var fs = new FileStream(rdlcFilePath, FileMode.Open);
        var report = new LocalReport();
        report.EnableExternalImages = true;
        report.LoadReportDefinition(fs);
        
        if (dsName1 != null && reportList1 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName1, reportList1));            
        }
        if (dsName2 != null && reportList2 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName2, reportList2));
            //report.AddDataSource(dsName2, reportList2);
        }
        if (dsName3 != null && reportList3 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName3, reportList3));
            //report.AddDataSource(dsName3, reportList3);
        }
        if (dsName4 != null && reportList4 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName4, reportList4));
            //report.AddDataSource(dsName4, reportList4);
        }
        if (parameters != null)
        {
            foreach (var parm in parameters)
            {
                report.SetParameters(new[] { new ReportParameter(parm.Key, parm.Value.ToString()) });
            }

        }
      
        var result = report.Render(_reportFormat);
        return result;
    }
    public byte[] GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, string dsName3 = null, object reportList1 = null, object reportList2 = null, object reportList3 = null, Dictionary<string, string> parameters = null)
    {
        //string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
        //string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
        string fileDirPath = AppDomain.CurrentDomain.BaseDirectory;
        string rdlcFilePath = Path.Combine(fileDirPath, "Reports", $"{reportName}.rdlc");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.GetEncoding("windows-1252");
        string _reportFormat = "pdf";
        switch (reportType)
        {
            case "xlsx":
                _reportFormat = "EXCELOPENXML";
                break;
            case "docx":
                _reportFormat = "WORDOPENXML";
                break;
            case "html":
                _reportFormat = "HTML5";
                break;
            case "pdf":
                _reportFormat = "PDF";
                break;
        }
        if (parameters == null)
            parameters = new Dictionary<string, string>();
        using var fs = new FileStream(rdlcFilePath, FileMode.Open);
        var report = new LocalReport();
        report.EnableExternalImages = true;
        report.LoadReportDefinition(fs);

        if (dsName1 != null && reportList1 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName1, reportList1));
        }
        if (dsName2 != null && reportList2 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName2, reportList2));
            //report.AddDataSource(dsName2, reportList2);
        }
        if (dsName3 != null && reportList3 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName3, reportList3));
            //report.AddDataSource(dsName3, reportList3);
        }
        
        if (parameters != null)
        {
            foreach (var parm in parameters)
            {
                report.SetParameters(new[] { new ReportParameter(parm.Key, parm.Value.ToString()) });
            }

        }

        var result = report.Render(_reportFormat);
        return result;
    }
    public byte[] GenerateReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, object reportList1 = null, object reportList2 = null, Dictionary<string, string> parameters = null)
    {
        //string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
        //string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
        string fileDirPath = AppDomain.CurrentDomain.BaseDirectory;
        string rdlcFilePath = Path.Combine(fileDirPath, "Reports", $"{reportName}.rdlc");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.GetEncoding("windows-1252");
        string _reportFormat = "pdf";
        switch(reportType)
        {
            case "xlsx":
                _reportFormat = "EXCELOPENXML";
                break;
            case "docx":
                _reportFormat = "WORDOPENXML";
                break;
            case "html":
                _reportFormat = "HTML5";
                break;
            case "pdf":
                _reportFormat = "PDF";
                break;
        }
        if (parameters == null)
            parameters = new Dictionary<string, string>();

        var report = new LocalReport();
        report.EnableExternalImages = true;
        using var fs = new FileStream(rdlcFilePath, FileMode.Open);
        report.LoadReportDefinition(fs);
       
        if (dsName1 != null && reportList1 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName1, reportList1));
        }
        if (dsName2 != null && reportList2 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName2, reportList2));
        }
        if (parameters != null)
        {
            foreach (var parm in parameters)
            {
                report.SetParameters(new[] { new ReportParameter(parm.Key, parm.Value.ToString()) });
            }

        }
        var result = report.Render(_reportFormat);
        return result;
    }


    public byte[] GenerateReportSubReportAsync(string reportName, string reportType, string dsName1 = null, string dsName2 = null, string dsName3 = null, string dsName4 = null, object reportList1 = null, object reportList2 = null, object reportList3 = null, object reportList4 = null, Dictionary<string, string> parameters = null)
    {
        string fileDirPath = Assembly.GetExecutingAssembly().Location.Replace("MyEnterprise.Server.UI.dll", string.Empty);
        string rdlcFilePath = $"{fileDirPath}Reports\\{reportName}.rdlc";
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding.GetEncoding("windows-1252");
        string _reportFormat = "pdf";
        switch (reportType)
        {
            case "xlsx":
                _reportFormat = "EXCELOPENXML";
                break;
            case "docx":
                _reportFormat = "WORDOPENXML";
                break;
            case "html":
                _reportFormat = "HTML5";
                break;
            case "pdf":
                _reportFormat = "PDF";
                break;
        }
        if (parameters == null)
            parameters = new Dictionary<string, string>();
        using var fs = new FileStream(rdlcFilePath, FileMode.Open);
        var report = new LocalReport();
        report.EnableExternalImages = true;
        report.LoadReportDefinition(fs);

        if (dsName1 != null && reportList1 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName1, reportList1));
        }
        if (dsName2 != null && reportList2 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName2, reportList2));
            //report.AddDataSource(dsName2, reportList2);
        }
        if (dsName3 != null && reportList3 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName3, reportList3));
            //report.AddDataSource(dsName3, reportList3);
        }
        if (dsName4 != null && reportList4 != null)
        {
            report.DataSources.Add(new ReportDataSource(dsName4, reportList4));
            //report.AddDataSource(dsName4, reportList4);
        }
        if (parameters != null)
        {
            foreach (var parm in parameters)
            {
                report.SetParameters(new[] { new ReportParameter(parm.Key, parm.Value.ToString()) });
            }

        }
       
            report.SubreportProcessing += (sender, e) =>
            {
                if (e.ReportPath == "Report1.rdlc") // Name of your subreport
                { 
                }
            };
         
        
      

        var result = report.Render(_reportFormat);
        return result;
    }

    private void Report_SubreportProcessing(object sender, SubreportProcessingEventArgs e)
    {
        int Id = int.Parse(e.Parameters["EmployeeId"].Values[0].ToString());
        var report = new LocalReport();

         
    }
    

    
}
