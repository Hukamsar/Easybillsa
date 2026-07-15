using AOne.DataAccess.Repository;
using AOne.DataAccess.Repository.IRepository;
using AOne.Models;
using AOne.Models.Entity;
using AOne.Models.ViewModels;
using EasyBill.Models.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AOne.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUsers>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<ItemMaster> ItemMasters { get; set; }
        public DbSet<ItemImage> ItemImages { get; set; }
        public DbSet<CategoryMaster> CategoryMasters { get; set; }
        public DbSet<Hsn> Hsns { get; set; }
        public DbSet<Supplier>Suppliers { get; set; } 
        public DbSet<Bank> Banks { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Division> Divisions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<CustomerOtp> CustomerOtps { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseItem> PurchaseItems { get; set; }
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<StockIssue> StockIssues { get; set; }
        public DbSet<StockIssueItem> StockIssueItems { get; set; }
        public DbSet<StockReturn> StockReturns { get; set; }
        public DbSet<StockReturnItem> StockReturnItems { get; set; }
        public DbSet<Stock> Stocks { get; set; }
        public DbSet<PurchaseReturn> PurchaseReturns { get; set; }
        public DbSet<PurchaseReturnItem> PurchaseReturnItems { get; set; }
        public DbSet<Sales> Saless { get; set; }
        public DbSet<SalesItem> SalesItems { get; set; }
        public DbSet<StockReceive> StockReceives { get; set; }
        public DbSet<StockReceiveItem> StockReceiveItems { get; set; }


        
        public DbSet<PharmacyDoctor> PharmacyDoctors { get; set; }
        public DbSet<PaymentVoucher> PaymentVoucher { get; set; }
        public DbSet<PaymentVoucherCategory> PaymentVoucherCategories { get; set; }
        public DbSet<Contra> Contras { get; set; }
        public DbSet<SalseSetting> SalseSettings { get; set; }
        public DbSet<PurchaseSetting> PurchaseSettings { get; set; }
        public DbSet<TermsConditions> TermsConditions { get; set; }
        public DbSet<ModeOfPayment> ModeOfPayments { get; set; }
        public DbSet<SalsePaymentDetails> SalsePaymentDetails { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<OfferItem> OfferItems { get; set; }
        public DbSet<PointSetting> pointSettings { get; set; }
        public DbSet<PointTransaction> pointTransactions { get; set; }
        public DbSet<Optical> Opticals { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> designations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<HoldSales> HoldSales { get; set; }
        public DbSet<HoldSalesItem> HoldSalesItems { get; set; }
        public DbSet<AccountGroup> AccountGroups { get; set; }
        public DbSet<ReceiveVoucher> ReceiveVouchers { get; set; }
        public DbSet<OpeningStock> OpeningStocks { get; set; }

        public DbSet<InvoiceThemeSetting> InvoiceThemeSettings { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<PurchaseChallan> PurchaseChallans { get; set; }
        public DbSet<PurchaseChallanItem> PurchaseChallanItems { get; set; }
        public DbSet<POWithAI> POWithAIs { get; set; }
        public DbSet<CurrentStock> CurrentStocks { get; set; }
        public DbSet<StockConversion> StockConversions { get; set; }
        public DbSet<CustomerAdvance> CustomerAdvances { get; set; }
        public DbSet<SupplierAdvance> SupplierAdvances { get; set; }
        public DbSet<TenantWalletHistory> TenantWalletHistories { get; set; }
        public DbSet<WalletMaster> WalletMasters { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<BranchItemMapping> BranchItemMappings { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<EmployeeTransferLog> EmployeeTransferLogs { get; set; }
        public DbSet<OfferStoreMapping> OfferStoreMappings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.GetProperty("Deleted") != null)
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var filter = Expression.Lambda(
                        Expression.Equal(
                            Expression.Property(parameter, "Deleted"),
                            Expression.Constant(null)
                        ),
                        parameter
                    );

                    builder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
            base.OnModelCreating(builder); 

            builder.Entity<PlanFeature>()
                .HasKey(pf => new { pf.PlanId, pf.FeatureId });
        }
        private void ApplyBaseEntityAuditAndTenant()
        {
            OnBeforeSaveChanges(); 
        }

        public override async Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            OnBeforeSaveChanges();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void OnBeforeSaveChanges()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "System"; 
            var tenantId = user?.FindFirst("TenantId")?.Value;
            var username = user?.Identity?.Name ?? _httpContextAccessor?.HttpContext?.Session?.GetString("UserName") ?? "System";

            // 1. Process BaseEntity audit fields and Soft Delete
            var baseEntityEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted))
                .ToList();

            foreach (var entry in baseEntityEntries)
            {
                var entity = (BaseEntity)entry.Entity;
                if (entity is IMayHaveTenant tenantEntity && string.IsNullOrEmpty(tenantEntity.TenantId))
                {
                    tenantEntity.TenantId = tenantId;
                }
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.Created = DateTime.Now;
                        entity.CreatedBy = userId;
                        break;

                    case EntityState.Modified:
                        entity.LastModified = DateTime.Now;
                        entity.LastModifiedBy = userId;
                        break;

                    case EntityState.Deleted:
                        entity.Deleted = DateTime.Now;
                        entity.DeletedBy = userId; 
                        entry.State = EntityState.Modified;
                        break;
                }
            }

            // 2. Process Audit Logging
            var auditEntries = new List<AuditLog>();
            var controllerName = _httpContextAccessor?.HttpContext?.Items["ControllerName"]?.ToString();
            var actionName = _httpContextAccessor?.HttpContext?.Items["ActionName"]?.ToString();

            // Detect changes after updating the state/fields above
            ChangeTracker.DetectChanges();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var entityName = entry.Entity.GetType().Name;
                var action = entry.State.ToString();
                string description = "";

                if (entry.State == EntityState.Modified)
                {
                    // Check if this was originally a delete that got converted to soft delete
                    var isSoftDelete = entry.Properties.Any(p => p.Metadata.Name == "Deleted" && p.IsModified && p.CurrentValue != null && p.OriginalValue == null);

                    if (isSoftDelete)
                    {
                        description = $"Deleted {entityName}";
                        action = "Deleted";
                    }
                    else
                    {
                        var changes = new List<string>();
                        foreach (var prop in entry.Properties)
                        {
                            // Skip metadata fields like LastModified, LastModifiedBy
                            if (prop.Metadata.Name == "LastModified" || prop.Metadata.Name == "LastModifiedBy")
                                continue;

                            if (prop.IsModified)
                            {
                                var original = prop.OriginalValue?.ToString();
                                var current = prop.CurrentValue?.ToString();
                                if (original == current) continue;

                                changes.Add($"{prop.Metadata.Name}: '{original}' -> '{current}'");
                            }
                        }

                        if (changes.Any())
                        {
                            description = $"Updated {entityName}. Changes: {string.Join(", ", changes)}";
                        }
                    }
                }
                else if (entry.State == EntityState.Added)
                {
                    description = $"Created new {entityName}";
                }
                else if (entry.State == EntityState.Deleted)
                {
                    description = $"Deleted {entityName}";
                }

                if (!string.IsNullOrEmpty(description))
                {
                    var auditLog = new AuditLog
                    {
                        UserId = userId == "System" ? null : userId,
                        Username = username,
                        Action = actionName ?? action,
                        ControllerName = controllerName,
                        ActionName = actionName ?? action,
                        Description = description,
                        Timestamp = DateTime.Now,
                        TenantId = tenantId
                    };
                    auditEntries.Add(auditLog);
                }
            }

            if (auditEntries.Any())
            {
                AuditLogs.AddRange(auditEntries);
            }
        }

        public override int SaveChanges()
        {
            ApplyBaseEntityAuditAndTenant();
            return base.SaveChanges();
        }

        //public override Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        //{
        //    ApplyBaseEntityAuditAndTenant();
        //    return base.SaveChangesAsync(cancellationToken);
        //}

    }
}

