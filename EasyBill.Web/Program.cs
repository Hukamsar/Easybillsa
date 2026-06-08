global using AOne.DataAccess.Data;
global using AOne.DataAccess.Repository;
global using AOne.DataAccess.Repository.IRepository;
global using AOne.Models;
global using AOne.Models.ViewModels;
global using EasyBill.Models.ViewModels;
global using AOne.Utility.Enums;
global using AOneWeb.Service.Report;
global using EasyBill.Models.Entity;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.EntityFrameworkCore;
global using Newtonsoft.Json;
using AOne.DataAccess.ProfileService; 
using AOneWeb.Service;
using AOneWeb.Service.AuthService;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using EasyBill.DataAccess.Repository;
using EasyBill.DataAccess.Repository.IRepository;
using EasyBill.DataAccess.StoredProcedures;
using EasyBill.UI.ModelBinders;
using EasyBill.UI.Service;
using EasyBill.UI.Service.Extensions;
using EasyBill.UI.Service.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] then your valid JWT token in the text input below.\nExample: \"Bearer your_token_here\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
//builder.Services.AddTransient<IApplicationDbContext>(provider => provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddIdentity<ApplicationUsers, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(2);
    options.SlidingExpiration = true;
});

builder.Services.AddSingleton<ITicketStore, DistributedCacheTicketStore>();

builder.Services
    .AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
    .Configure<ITicketStore>((options, ticketStore) =>
    {
        options.SessionStore = ticketStore;
    });

builder.Services.AddAuthentication()
    .AddJwtBearer(bearer =>
    {
        bearer.RequireHttpsMetadata = false;
        bearer.SaveToken = true;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("S0M3RAN0MS3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!3CR3T!1!MAG1C!1!")),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };


    });
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(2); // Set session timeout
    options.Cookie.HttpOnly = true; // Security: prevents JS access
    options.Cookie.IsEssential = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Your Angular app URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.SchemaName = "dbo";
    options.TableName = "AuthTicketCache";
});
builder.Services.AddHttpContextAccessor();
//builder.Services.AddControllersWithViews();
builder.Services.AddControllersWithViews(options =>
{
    // ✅ Register custom model binder - Add this line
    options.ModelBinderProviders.Insert(0, new ExpiryDateModelBinderProvider());
    // ✅ Register PlanAccessFilter
    options.Filters.Add<EasyBill.UI.Helpers.PlanAccessFilter>();
});

builder.Services.AddAuthorization();
builder.Services.RegisterApplicationServices();
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EasyBill.UI.Helpers.LocalEmailSender>();
builder.Services.AddHealthChecks();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    if (context.Database.IsSqlServer())
    {
        context.Database.Migrate();
        await context.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[AuthTicketCache]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AuthTicketCache]
    (
        [Id] NVARCHAR(449) NOT NULL,
        [Value] VARBINARY(MAX) NOT NULL,
        [ExpiresAtTime] DATETIMEOFFSET NOT NULL,
        [SlidingExpirationInSeconds] BIGINT NULL,
        [AbsoluteExpiration] DATETIMEOFFSET NULL,
        CONSTRAINT [PK_AuthTicketCache] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_AuthTicketCache_ExpiresAtTime]
        ON [dbo].[AuthTicketCache]([ExpiresAtTime]);
END");
        await ItemMasterModuleStoredProcedureInstaller.EnsureInstalledAsync(context);
        await PurchaseModuleStoredProcedureInstaller.EnsureInstalledAsync(context);
        await SalesModuleStoredProcedureInstaller.EnsureInstalledAsync(context);
        await DashboardModuleStoredProcedureInstaller.EnsureInstalledAsync(context);
    }
    await SeedService.SeedSampleDataAsync(context);
    await SeedService.SeedDatabase(services);
} 
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "swagger"; // Keep Swagger at /swagger
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles();
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".avif"] = "image/avif";
provider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();


app.UseCors("AllowAngularApp");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<EasyBill.UI.Service.UserProfileMiddleware>();
app.UseMiddleware<JwtSlidingExpirationMiddleware>();
app.UseHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
