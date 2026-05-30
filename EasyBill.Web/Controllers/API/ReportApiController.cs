using AOneWeb.Helpers;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyBill.UI.Controllers.API
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportApiController : ControllerBase
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

        public ReportApiController(
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

        // -------------------------------
        // SALES DETAILS REPORT
        // -------------------------------

        [HttpGet("SalseDetailsReport/{id}")]
        public async Task<IActionResult> SalseDetailsReport(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales id" });

            var dataSet = await GetSalesDataAsync(id);
            var dataSet1 = await GetSalesItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalseReportName,
                reportType,
                ReportHelper.GetReportDs(SalseDetailsReportName),
                "SalseItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return Ok(new { success = true, report = Convert.ToBase64String(reportBytes) });
        }

        [HttpGet("SalseDetailsReportPdf/{id}")]
        public async Task<IActionResult> SalseDetailsReportPdf(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales id" });

            var dataSet = await GetSalesDataAsync(id);
            var dataSet1 = await GetSalesItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalseReportName,
                reportType,
                ReportHelper.GetReportDs(SalseDetailsReportName),
                "SalseItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return File(reportBytes, "application/pdf", $"{SalseReportName}.pdf");
        }

        // -------------------------------
        // SALES REPORT
        // -------------------------------

        [HttpGet("SalseReport/{id}")]
        public async Task<IActionResult> SalseReport(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales id" });

            var dataSet = await GetSalesDataAsync(id);
            var dataSet1 = await GetSalesItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalseDetailsReportName,
                reportType,
                ReportHelper.GetReportDs(SalseDetailsReportName),
                "SalseItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return Ok(new { success = true, report = Convert.ToBase64String(reportBytes) });
        }

        [HttpGet("SalseReportPdf/{id}")]
        public async Task<IActionResult> SalseReportPdf(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales id" });

            var dataSet = await GetSalesDataAsync(id);
            var dataSet1 = await GetSalesItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalseDetailsReportName,
                reportType,
                ReportHelper.GetReportDs(SalseDetailsReportName),
                "SalseItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return File(reportBytes, "application/pdf", $"{SalseDetailsReportName}.pdf");
        }

        // -------------------------------
        // SALES ORDER REPORT
        // -------------------------------

        [HttpGet("SalseOrderReport/{id}")]
        public async Task<IActionResult> SalseOrderReport(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales order id" });

            var dataSet = await GetSalesOrderDataAsync(id);
            var dataSet1 = await GetSalesOrderItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalesOrderReportName,
                reportType,
                ReportHelper.GetReportDs(SalesOrderReportName),
                "dsSalesOrderItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return Ok(new { success = true, report = Convert.ToBase64String(reportBytes) });
        }

        [HttpGet("SalseOrderReportPdf/{id}")]
        public async Task<IActionResult> SalseOrderReportPdf(int? id)
        {
            if (id == null) return BadRequest(new { success = false, message = "Invalid sales order id" });

            var dataSet = await GetSalesOrderDataAsync(id);
            var dataSet1 = await GetSalesOrderItemData(id);

            string reportType = reportTypeEnum.ToString();

            var reportBytes = _reportService.GenerateReportAsync(
                SalesOrderReportName,
                reportType,
                ReportHelper.GetReportDs(SalesOrderReportName),
                "dsSalesOrderItemDetails",
                dataSet,
                dataSet1,
                new Dictionary<string, string>()
            );

            return File(reportBytes, "application/pdf", $"{SalesOrderReportName}.pdf");
        }

        // -------------------------------
        // PRIVATE HELPERS (unchanged)
        // -------------------------------
        private async Task<object> GetSalesDataAsync(int? salseId)
        {
            var userId = _signmanager.Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _usermanager.FindByIdAsync(userId);
            var tenantdata = await _tenantService.GetById(user?.TenantId);
            if (user?.TenantId == null) return null;
            var salse = await _salesServices.GetById(salseId);
            var termsconditionsdata = (await _termconditionsservice.GetAll()).FirstOrDefault(x => x.TenantId == user.TenantId);

            return new List<SalesVM>
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
                    Tenantname = tenantdata.Name,
                    TenantAddress = tenantdata.FullAddress,
                    TenantEmail = tenantdata.Email,
                    TenantPhone = tenantdata.Phone,
                    TermConditions = termsconditionsdata?.Name
                }
            };
        }

        private async Task<object> GetSalesItemData(int? SalseId)
        {
            var salse = await _salesServices.GetById(SalseId);
            return salse.SalesItems.Select(p => new SalesItemVM
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
        }

        private async Task<object> GetSalesOrderDataAsync(int? salseId)
        {
            var userId = _signmanager.Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _usermanager.FindByIdAsync(userId);
            var tenantdata = await _tenantService.GetById(user?.TenantId);
            if (user?.TenantId == null) return null;
            var salse = await _salesOrderRepo.GetById(salseId);
            var termsconditionsdata = (await _termconditionsservice.GetAll()).FirstOrDefault(x => x.TenantId == user.TenantId);

            return new List<SalesOrderVM>
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
        }

        private async Task<object> GetSalesOrderItemData(int? SalseId)
        {
            var salse = await _salesOrderRepo.GetById(SalseId);
            return salse.salesOrderItems.Select(p => new SalesOrderItemVM
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
        }
    }
}
