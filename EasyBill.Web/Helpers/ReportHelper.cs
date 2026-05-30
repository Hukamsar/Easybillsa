

namespace AOneWeb.Helpers;

public enum ReportNames
{ 
    SalseDetailsReport ,
    SalesOrderDetailsReport,
    SalseReport
}
public class ReportHelper
{
    private static readonly Dictionary<string, string> ReportNameDs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {

           {nameof(ReportNames.SalseDetailsReport),"SalseDetails" },
            {nameof(ReportNames.SalseReport),"SalseDetails" },
           {nameof(ReportNames.SalesOrderDetailsReport),"dsSalesOrderDetails" }
        };



    public static string GetReportDs(string reportName)
    {
        if (ReportNameDs.ContainsKey(reportName))
        {
            return ReportNameDs[reportName];
        }
        return null;
    }
}
