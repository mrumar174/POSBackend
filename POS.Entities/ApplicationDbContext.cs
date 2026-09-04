using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Entities.Catalog;
using POS.Entities.Common;
using POS.Entities.Finance;
using POS.Entities.Identity;
using POS.Entities.Inventory;
using POS.Entities.Purchasing;
using POS.Entities.Sales;
using POS.Entities.Settings;
using POS.Entities.Tenancy;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace POS.Entities.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ICurrentUserService _currentUser;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
            : base(options)
        {
            _currentUser = currentUser;
        }

        // Tenancy
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Shop> Shops => Set<Shop>();
        public DbSet<UserShop> UserShops => Set<UserShop>();

        // Identity
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        // Catalog
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();

        // Purchasing
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseDetail> PurchaseDetails => Set<PurchaseDetail>();
        public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
        public DbSet<PurchaseReturnDetail> PurchaseReturnDetails => Set<PurchaseReturnDetail>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

        // Sales
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
        public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
        public DbSet<SaleReturnDetail> SaleReturnDetails => Set<SaleReturnDetail>();

        // Inventory
        public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
        public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
        public DbSet<StockAdjustmentDetail> StockAdjustmentDetails => Set<StockAdjustmentDetail>();

        // Finance
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
        public DbSet<DailyCashClosing> DailyCashClosings => Set<DailyCashClosing>();

        // Settings
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------------------------------------------------------------
            // 1. Decimal precision: money = (18,2), quantities = (18,3)
            //    (matches the DECIMAL(18,2)/DECIMAL(18,3) rule from the DB doc)
            // ---------------------------------------------------------------
            var quantityProperties = new HashSet<string>
            {
                nameof(Product.MinimumStock),
                nameof(PurchaseDetail.Quantity),
                nameof(SaleDetail.Quantity),
                nameof(PurchaseReturnDetail.Quantity),
                nameof(SaleReturnDetail.Quantity),
                nameof(StockTransaction.QuantityIn),
                nameof(StockTransaction.QuantityOut),
                nameof(StockAdjustmentDetail.SystemQuantity),
                nameof(StockAdjustmentDetail.PhysicalQuantity),
                nameof(StockAdjustmentDetail.DifferenceQuantity),
            };

            foreach (var property in modelBuilder.Model.GetEntityTypes()
                         .SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType(quantityProperties.Contains(property.Name) ? "decimal(18,3)" : "decimal(18,2)");
            }

            // ---------------------------------------------------------------
            // 2. Global query filters: Tenant isolation, Shop isolation, soft delete.
            //    Applied via reflection so every ITenantScoped/IShopScoped entity
            //    is covered automatically — nobody has to remember to add a new
            //    entity here.
            // ---------------------------------------------------------------
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                if (typeof(IShopScoped).IsAssignableFrom(clrType))
                {
                    typeof(ApplicationDbContext)
                        .GetMethod(nameof(ApplyShopFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(clrType)
                        .Invoke(this, new object[] { modelBuilder });
                }
                else if (typeof(ITenantScoped).IsAssignableFrom(clrType))
                {
                    typeof(ApplicationDbContext)
                        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(clrType)
                        .Invoke(this, new object[] { modelBuilder });
                }
                else if (typeof(AuditableEntity).IsAssignableFrom(clrType))
                {
                    // Not tenant/shop scoped (e.g. Permission) but still soft-deletable.
                    typeof(ApplicationDbContext)
                        .GetMethod(nameof(ApplyActiveFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                        .MakeGenericMethod(clrType)
                        .Invoke(this, new object[] { modelBuilder });
                }
            }

            // ---------------------------------------------------------------
            // 3. Composite keys for join tables
            // ---------------------------------------------------------------
            modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });
            modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
            modelBuilder.Entity<UserShop>().HasKey(x => new { x.UserId, x.ShopId });

            // ---------------------------------------------------------------
            // 4. Unique indexes — moved from "global unique" (original single-shop
            //    script) to "unique per Tenant" (master data) or "unique per Shop"
            //    (per-branch documents like invoice numbers).
            // ---------------------------------------------------------------
            modelBuilder.Entity<Tenant>().HasIndex(x => x.Slug).IsUnique();
            modelBuilder.Entity<Tenant>().HasIndex(x => x.Code).IsUnique();
            modelBuilder.Entity<Shop>().HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

            modelBuilder.Entity<Category>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<Brand>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<Unit>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<Unit>().HasIndex(x => new { x.TenantId, x.ShortName }).IsUnique();
            modelBuilder.Entity<PaymentMethod>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<ExpenseCategory>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(x => x.Name).IsUnique(); // global — not tenant-scoped
            modelBuilder.Entity<User>().HasIndex(x => new { x.TenantId, x.UserName }).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(x => new { x.TenantId, x.ProductCode }).IsUnique();
            modelBuilder.Entity<ProductBarcode>().HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique();

            modelBuilder.Entity<Purchase>().HasIndex(x => new { x.ShopId, x.InvoiceNo }).IsUnique();
            modelBuilder.Entity<Sale>().HasIndex(x => new { x.ShopId, x.InvoiceNo }).IsUnique();
            modelBuilder.Entity<PurchaseReturn>().HasIndex(x => new { x.ShopId, x.ReturnNo }).IsUnique();
            modelBuilder.Entity<SaleReturn>().HasIndex(x => new { x.ShopId, x.ReturnNo }).IsUnique();
            modelBuilder.Entity<StockAdjustment>().HasIndex(x => new { x.ShopId, x.AdjustmentNo }).IsUnique();
            modelBuilder.Entity<Expense>().HasIndex(x => new { x.ShopId, x.ExpenseNo }).IsUnique();
            modelBuilder.Entity<SupplierPayment>().HasIndex(x => new { x.ShopId, x.PaymentNo }).IsUnique();
            modelBuilder.Entity<CashTransaction>().HasIndex(x => new { x.ShopId, x.TransactionNo }).IsUnique();
            modelBuilder.Entity<DailyCashClosing>().HasIndex(x => new { x.ShopId, x.ClosingDate }).IsUnique();

            // ---------------------------------------------------------------
            // 5. Enum columns stored as readable strings (mirrors the original
            //    CHECK (TransactionType IN (...)) constraints).
            // ---------------------------------------------------------------
            modelBuilder.Entity<StockTransaction>()
                .Property(x => x.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(30);

            modelBuilder.Entity<CashTransaction>()
                .Property(x => x.TransactionType)
                .HasConversion<string>()
                .HasMaxLength(30);

            // ---------------------------------------------------------------
            // 6. No cascade deletes anywhere. We soft-delete (IsActive = false),
            //    so cascading physical deletes would fight that and SQL Server
            //    will also reject some of the multi-path cascade graphs here anyway.
            // ---------------------------------------------------------------
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantScoped
        {
            var tenantId = _currentUser.TenantId;
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                e.TenantId == tenantId && EF.Property<bool>(e, nameof(AuditableEntity.IsActive)));
        }

        private void ApplyShopFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IShopScoped
        {
            var tenantId = _currentUser.TenantId;
            var shopId = _currentUser.ShopId;
            // ShopId == null (e.g. a tenant owner viewing "all branches") means don't
            // restrict by shop — TenantId isolation still applies.
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                e.TenantId == tenantId
                && (shopId == null || e.ShopId == shopId)
                && EF.Property<bool>(e, nameof(AuditableEntity.IsActive)));
        }

        private void ApplyActiveFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => EF.Property<bool>(e, nameof(AuditableEntity.IsActive)));
        }

        public override int SaveChanges()
        {
            ApplyAuditAndTenantStamping();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditAndTenantStamping();
            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Stamps audit fields and TenantId/ShopId from the current logged-in
        /// user (never from client input), and converts hard deletes into soft
        /// deletes (IsActive = false) per the "soft delete instead of physical
        /// delete" rule in the DB design doc.
        /// </summary>
        private void ApplyAuditAndTenantStamping()
        {
            var now = DateTime.Now;
            var userId = _currentUser.UserId;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.AddedOn = now;
                        entry.Entity.AddedBy = userId;
                        entry.Entity.IsActive = true;

                        if (entry.Entity is ITenantScoped tenantScoped && tenantScoped.TenantId == 0)
                            tenantScoped.TenantId = _currentUser.TenantId;

                        if (entry.Entity is IShopScoped shopScoped && shopScoped.ShopId == 0 && _currentUser.ShopId.HasValue)
                            shopScoped.ShopId = _currentUser.ShopId.Value;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedOn = now;
                        entry.Entity.UpdatedBy = userId;
                        break;

                    case EntityState.Deleted:
                        // Soft delete: never physically remove a row.
                        entry.State = EntityState.Modified;
                        entry.Entity.IsActive = false;
                        entry.Entity.DeletedOn = now;
                        entry.Entity.DeletedBy = userId;
                        break;
                }
            }
        }
    }
}



//Add - Migration InitialCreate - Project POS.Entities - StartupProject POS.Web - OutputDir Data / Migrations
//Update - Database - Project POS.Entities - StartupProject POS.Web