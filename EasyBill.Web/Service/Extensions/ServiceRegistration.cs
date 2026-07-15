using AOne.DataAccess.ProfileService;
using AOneWeb.Service.AuthService;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.UI.Service.Auth;
using EasyBill.UI.Service.ExcelService;
using EasyBill.UI.Service.Loyalty;
using EasyBill.UI.Service.Sms;
using EasyBill.UI.Service.Whatsapp;

namespace EasyBill.UI.Service.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IMenuService, MenuService>();
            services.AddTransient<IReportService, ReportService>();
            services.AddScoped<AuthService>();
            services.AddScoped<UserLoginAuthService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITenantAccessor, TenantAccessor>();
            services.AddScoped<IItemMasterRepository, ItemMasterRepository>();
            services.AddScoped<IItemImageRepository, ItemImageRepository>();
            services.AddScoped<ICategoryMasterRepository, CategoryMasterRepository>();
            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<IBankRepository, BankRepository>();
            services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            services.AddScoped<IHSNRepository, HSNRepository>();
            services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
            services.AddScoped<IDivisionRepository, DivisionRepository>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceItemRepository, InvoiceItemRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IPurchaseItemRepository, PurchaseItemRepository>();
            services.AddScoped<IBatchRepository, BatchRepository>();
            services.AddScoped<ICountryRepository, CountryRepository>();
            services.AddScoped<ICityRepository, CityRepository>();
            services.AddScoped<IStateRepository, StateRepository>();
            services.AddScoped<IStockIssueRepository, StockIssueRepository>();
            services.AddScoped<ITenantRegistrationRepository, TenantRegistrationRepository>();
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUsers>, CustomClaimsPrincipalFactory>();
            services.AddScoped<IStockReturnRepository, StockReturnRepository>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IPurchaseReturnRepository, PurchaseReturnRepository>();
            services.AddScoped<ISalesRepository, SalesRepository>();
            services.AddScoped<IStockReceiveRepository, StockReceiveRepository>();
            services.AddScoped<IPharmacyDoctorRepository, PharmacyDoctorRepository>();
            services.AddScoped<IpaymentVoucherRepository, PaymentVoucherRepository>();
            services.AddScoped<IContraRepository, ContraRepository>();
            services.AddScoped<IPaymentVoucherCategoryRepository, PaymentVoucherCategoryRepository>();
            services.AddScoped<ISalseSettingRepository, SalseSettingRepository>();
            services.AddScoped<ITermConditionsRepository, TermConditionsRepository>();
            services.AddScoped<IModeOfPaymentRepository, ModeOfPaymentRepository>();
            services.AddScoped<ISalsePaymentDetailsRepository, SalsePaymentDetailsRepository>();
            services.AddScoped<IExcelService, ExcelServices>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<IPointSettingRepository, PointSettingRepository>();
            services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
            services.AddScoped<IOpticalRepository, OpticalRepository>();
            services.AddScoped<ITenantRepository, ClinicRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IDesignationRepository, DesignationRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IAccountGroupRepository, AccountGroupRepository>();
            services.AddScoped<IReceiveVoucherRepository, ReceiveVoucherRepository>();
            services.AddScoped<IOpeningStockRepository, OpeningStockRepository>();
            services.AddScoped<IPurchaseSettingRepository, PurchaseSettingRepository>();
            services.AddScoped<IInvoiceThemeSettingRepository, InvoiceThemeSettingRepository>();
            services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
            services.AddScoped<IPurchaseChallanRepository, PurchaseChallanRepository>();
            services.AddScoped<IHomeIndexDashboardDataRepository, HomeIndexDashboardDataRepository>();
            services.AddScoped<IPOWithAIRepository, POWithAIRepository>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<ICustomerAdvanceRepository, CustomerAdvanceRepository>();
            services.AddScoped<ISupplierAdvanceRepository, SupplierAdvanceRepository>();
            
            services.AddScoped<SmsService>();
            services.AddScoped<CustomerLoyaltyService>();
            services.AddHttpClient<WhatsAppService>();
            return services;
        }
    }
}
