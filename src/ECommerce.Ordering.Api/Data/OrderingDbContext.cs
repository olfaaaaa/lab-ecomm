using ECommerce.Ordering.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Ordering.Api.Data;

public class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Customer).HasMaxLength(200).IsRequired();
            entity.Property(o => o.PromoCode).HasMaxLength(50);
            entity.Ignore(o => o.Subtotal);
            entity.Ignore(o => o.Discount);
            entity.Ignore(o => o.Total);
            entity.HasMany(o => o.Items).WithOne().OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasKey(p => p.Code);
            entity.Property(p => p.Code).HasMaxLength(50);
        });

        // Demo codes so the feature is usable immediately.
        modelBuilder.Entity<PromoCode>().HasData(
            new PromoCode { Code = "WELCOME10", PercentOff = 10, IsActive = true },
            new PromoCode { Code = "SUMMER20", PercentOff = 20, IsActive = true },
            new PromoCode { Code = "OLD50", PercentOff = 50, IsActive = true, ExpiresAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero) },
            new PromoCode { Code = "PAUSED30", PercentOff = 30, IsActive = false });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });
    }
}
