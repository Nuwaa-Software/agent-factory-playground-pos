using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniPos.Domain.Catalog;
using MiniPos.Domain.Common;
using MiniPos.Domain.Companies;
using MiniPos.Domain.Customers;
using MiniPos.Domain.Sales;
using MiniPos.Domain.Shifts;
using MiniPos.Domain.Users;
using MiniPos.Infrastructure.Tenancy;

namespace MiniPos.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<CompanyConfig> CompanyConfigs => Set<CompanyConfig>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Refund> Refunds => Set<Refund>();

    /// <summary>Read by the global query filters below, per DbContext instance (i.e. per request).</summary>
    public Guid CurrentCompanyId => tenant.CompanyId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Company>(b =>
        {
            b.Property(c => c.NameEn).HasMaxLength(200);
            b.Property(c => c.NameAr).HasMaxLength(200);
            b.Property(c => c.VatNumber).HasMaxLength(15);
        });

        modelBuilder.Entity<Product>(b =>
        {
            b.Property(p => p.NameEn).HasMaxLength(200);
            b.Property(p => p.NameAr).HasMaxLength(200);
            b.Property(p => p.Sku).HasMaxLength(64);
            b.HasIndex(p => new { p.CompanyId, p.Sku }).IsUnique();
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.Property(c => c.NameEn).HasMaxLength(200);
            b.Property(c => c.NameAr).HasMaxLength(200);
            b.Property(c => c.Description).HasMaxLength(1024);
            b.HasIndex(c => new { c.CompanyId, c.NameEn }).IsUnique();
        });

        modelBuilder.Entity<Sale>(b =>
        {
            b.HasIndex(s => new { s.CompanyId, s.Number }).IsUnique();
            b.OwnsMany(s => s.Lines, l =>
            {
                l.WithOwner().HasForeignKey("SaleId");
                l.Property(x => x.ProductNameEn).HasMaxLength(200);
                l.Property(x => x.ProductNameAr).HasMaxLength(200);
                l.Property(x => x.DiscountHalalas).HasColumnName("DiscountHalalas");
                l.Property(x => x.DiscountReason).HasMaxLength(512);
                l.Ignore(x => x.LineNetHalalas);
            });
            b.Property(s => s.SaleDiscountHalalas).HasDefaultValue(0L);
            b.Property(s => s.SaleDiscountReason).HasMaxLength(512);
            b.OwnsMany(s => s.PaymentBreakdown, e =>
            {
                e.WithOwner().HasForeignKey("SaleId");
                e.Property(x => x.PaymentType).HasConversion<long>();
                e.Property(x => x.AmountHalalas);
            });
        });

        modelBuilder.Entity<Shift>(b =>
        {
            b.Property(s => s.CashierName).HasMaxLength(100);
            b.HasIndex(s => new { s.CompanyId, s.OpenedAt }).IsDescending();
        });

        modelBuilder.Entity<CompanyConfig>(b =>
        {
            b.Property(c => c.Key).HasMaxLength(128);
            b.Property(c => c.Value).HasMaxLength(1024);
            b.HasIndex(c => new { c.CompanyId, c.Key }).IsUnique();
        });

        modelBuilder.Entity<Customer>(b =>
        {
            b.Property(c => c.Name).HasMaxLength(200);
            b.Property(c => c.Phone).HasMaxLength(32);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.NameEn).HasMaxLength(200);
            b.Property(u => u.NameAr).HasMaxLength(200);
            b.HasIndex(u => u.CompanyId);
        });

        modelBuilder.Entity<Refund>(b =>
        {
            b.Property(r => r.Reason).HasMaxLength(1024);
            b.Property(r => r.RefundedBy).HasMaxLength(100);
        });

        // Multi-tenancy, read side: every ITenantScoped entity only ever returns the current company's rows.
        foreach (var entity in modelBuilder.Model.GetEntityTypes()
                     .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType) && e.BaseType is null))
        {
            modelBuilder.Entity(entity.ClrType).HasQueryFilter(TenantFilter(entity.ClrType));
        }
    }

    private LambdaExpression TenantFilter(Type type)
    {
        var e = Expression.Parameter(type, "e");
        var companyId = Expression.Property(e, nameof(ITenantScoped.CompanyId));
        var current = Expression.Property(Expression.Constant(this), nameof(CurrentCompanyId));
        return Expression.Lambda(Expression.Equal(companyId, current), e);
    }
}