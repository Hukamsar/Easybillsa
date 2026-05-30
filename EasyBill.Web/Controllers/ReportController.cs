using AOneWeb.Helpers;
using DocumentFormat.OpenXml.Office2010.Excel;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportService _reportService; 
        private readonly ISalesRepository _salesServices;
        private readonly SignInManager<ApplicationUsers> _signmanager;
        private readonly UserManager<ApplicationUsers> _usermanager;
        private readonly ITenantRegistrationRepository _tenantService;
        private readonly ITermConditionsRepository _termconditionsservice;
        private readonly ISalesOrderRepository _salesOrderRepo;
        private ReportTypeExtensions reportTypeEnum { get; set; } = ReportTypeExtensions.pdf;
        public string SalseDetailsReportName = "SalseDetailsReport";
        public string SalseReportName = "SalseReport";
        public string SalesOrderReportName = "SalesOrderDetailsReport";
        public ReportController(
            IReportService reportService, 
            ISalesRepository salesServices,
            SignInManager<ApplicationUsers> signmanager,
            UserManager<ApplicationUsers> usermanager,
            ITenantRegistrationRepository tenantservice,
            ITermConditionsRepository termconditionsservice,
            ISalesOrderRepository salesOrderRepo)
        {
            _reportService = reportService; 
            _salesServices = salesServices;
            _signmanager = signmanager;
            _usermanager = usermanager;
            _tenantService = tenantservice;
            _termconditionsservice = termconditionsservice;
            _salesOrderRepo = salesOrderRepo;
        }
        public async Task<IActionResult> SalseDetailsReport(int? id)
        {
            try
            {
                if (id == null) return BadRequest();

                var dataSet = await GetSalesDataAsync(id);
                var dataSet1 = await GetSalesItemData(id);

                var filter = new Dictionary<string, string>();
                string reportType = reportTypeEnum.ToString(); // Make sure `reportTypeEnum` is defined and set

                var reportBytes = _reportService.GenerateReportAsync(SalseReportName, reportType, ReportHelper.GetReportDs(SalseReportName), "SalseItemDetails", dataSet, dataSet1, filter);

                var fileExtension = ReportService.GetReportTypeExtension(reportType);
                var fileName = $"{SalseReportName}.{fileExtension}";

                // Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
                Response.Headers["Content-Disposition"] = "inline";
                return File(reportBytes, "application/pdf");
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<IActionResult> SalseReport(int? id)
        {
            try
            {
                if (id == null) return BadRequest();
                 
                var dataSet = await GetSalesDataAsync(id);
                var dataSet1 =await GetSalesItemData(id);

                var filter = new Dictionary<string, string>();
                string reportType = reportTypeEnum.ToString(); // Make sure `reportTypeEnum` is defined and set

                var reportBytes = _reportService.GenerateReportAsync(SalseDetailsReportName, reportType, ReportHelper.GetReportDs(SalseDetailsReportName), "SalseItemDetails", dataSet, dataSet1, filter);

                var fileExtension = ReportService.GetReportTypeExtension(reportType);
                var fileName = $"{SalseDetailsReportName}.{fileExtension}";

                // Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
                Response.Headers["Content-Disposition"] = "inline";
                return File(reportBytes, "application/pdf");
            }
            catch (Exception ex)
            {

                return Content(ex.ToString());
            }
        }


        private async Task<object> GetSalesDataAsync(int? salseId)
        {
            var userId = _signmanager.Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _usermanager.FindByIdAsync(userId);
            var tenantdata = await _tenantService.GetById(user?.TenantId);
            if (user?.TenantId == null) return null;
           // var salse = await _salesServices.GetById(salseId);

            var salse = await _salesServices.GetById(salseId);
            if (salse == null)
                throw new Exception($"Sales not found for id {salseId}");
            var termsconditionsdata = (await _termconditionsservice.GetAll()).Where(x => x.TenantId == user.TenantId).FirstOrDefault();
            List<SalesVM> objList = new List<SalesVM>
            {
                new SalesVM
                {
                    BillNo = salse.BillNo,
                    BillDate = salse.BillDate,
                    CustomerName = salse.Customers?.Name ?? "",
                    Address = salse.Customers?.Address ?? "",
                    MobileNo = salse.Customers?.PhoneNo ?? "",
                    DoctorName = salse.PharmacyDoctor?.Name ?? "",
                    DoctorMobileNumber = salse.DoctorMobileNumber ?? "",
                    DoctorRegNumber = salse.DoctorRegNumber ?? "",
                    Total = salse.Total,
                    discountPercent = salse.discountPercent,
                    discountAmount = salse.discountAmount,
                    Totaldiscount = salse.Totaldiscount,
                    TotalGstAmt = salse.TotalGstAmt,
                    TotalPayable = salse.TotalPayable,
                    //Tenantname = tenantdata.Name,
                    Tenantname = tenantdata?.Name ?? "",
                    TenantAddress = tenantdata?.FullAddress ?? "",
                    TenantEmail = tenantdata ?.Email ?? "",
                    TenantPhone = tenantdata?.Phone ?? "",
                    TermConditions = termsconditionsdata?.Name ?? ""
                }
            };
            return objList;
        }


        private async Task<object> GetSalesItemData(int? SalseId)
        {
           // var salse = await _salesServices.GetById(SalseId);

            var salse = await _salesServices.GetById(SalseId);
            if (salse == null)
                throw new Exception($"Sales not found for id {SalseId}");

            var salesItems = salse.SalesItems ?? new List<SalesItem>();
            List<SalesItemVM> objList = salse.SalesItems?.Select(p => new SalesItemVM
            {
                ItemName = p.ItemMaster?.Name ?? "",
                Batch = p.Batch,
                Qty = p.Qty,
                Rate = p.Rate,
                Gst = p.Gst,
                Discount = p.Discount,
                Amount = p.Amount,
                Expirydate = p.Expirydate,
                Mrp = p.Mrp
            }).ToList() ?? new List<SalesItemVM>();
            return objList.ToList();
        }

        public async Task<IActionResult> SalseOrderReport(int? id)
        {
            try
            {
                if (id == null) return BadRequest();

                var dataSet = await GetSalesOrderDataAsync(id);
                var dataSet1 = await GetSalesOrderItemData(id);

                var filter = new Dictionary<string, string>();
                string reportType = reportTypeEnum.ToString(); // Make sure `reportTypeEnum` is defined and set

                var reportBytes = _reportService.GenerateReportAsync(SalesOrderReportName, reportType, ReportHelper.GetReportDs(SalesOrderReportName), "dsSalesOrderItemDetails", dataSet, dataSet1, filter);

                var fileExtension = ReportService.GetReportTypeExtension(reportType);
                var fileName = $"{SalesOrderReportName}.{fileExtension}";

                // Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
                Response.Headers["Content-Disposition"] = "inline";
                return File(reportBytes, "application/pdf");
            }
            catch (Exception)
            {

                throw;
            }
        }


        private async Task<object> GetSalesOrderDataAsync(int? salseId)
        {
            var userId = _signmanager.Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _usermanager.FindByIdAsync(userId);
            var tenantdata = await _tenantService.GetById(user?.TenantId);
            if (user?.TenantId == null) return null;
            var salse = await _salesOrderRepo.GetById(salseId);
            var termsconditionsdata = (await _termconditionsservice.GetAll()).Where(x => x.TenantId == user.TenantId).FirstOrDefault();
            List<SalesOrderVM> objList = new List<SalesOrderVM>
            {
                new SalesOrderVM
                {
                    BillNo = salse.BillNo,
                    BillDate = salse.BillDate,
                    CustomerName = salse.Customers?.Name ?? "",
                    Address = salse.Customers?.Address ?? "",
                    MobileNo = salse.Customers?.PhoneNo ?? "",
                    DoctorName = salse.PharmacyDoctor?.Name ?? "",
                    DoctorMobileNumber = salse.DoctorMobileNumber ?? "",
                    DoctorRegNumber = salse.DoctorRegNumber ?? "",
                    Total = salse.Total,
                    discountPercent = salse.discountPercent,
                    discountAmount = salse.discountAmount,
                    Totaldiscount = salse.Totaldiscount,
                    TotalGstAmt = salse.TotalGstAmt,
                    TotalPayable = salse.TotalPayable,
                    Tenantname = tenantdata.Name,
                    TenantAddress = tenantdata.FullAddress,
                    TenantEmail = tenantdata.Email,
                    TenantPhone = tenantdata.Phone,
                    TermConditions = termsconditionsdata?.Name
                }
            };
            return objList;
        }


        private async Task<object> GetSalesOrderItemData(int? SalseId)
        {
            var salse = await _salesOrderRepo.GetById(SalseId);
            List<SalesOrderItemVM> objList = salse.salesOrderItems.Select(p => new SalesOrderItemVM
            {
                ItemName = p.ItemMaster?.Name ?? "",
                Batch = p.Batch,
                Qty = p.Qty,
                Rate = p.Rate,
                Gst = p.Gst,
                Discount = p.Discount,
                Amount = p.Amount,
                Expirydate = p.Expirydate,
                Mrp = p.Mrp
            }).ToList();
            return objList.ToList();
        }

    }
}
