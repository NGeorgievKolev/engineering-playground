using Microsoft.EntityFrameworkCore;
using OrderProcessing.SimpleApi.Models;

namespace OrderProcessing.SimpleApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store EUR monetary values as integer cents in SQLite.
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasConversion(
                value => checked((long)(value * 100m)),
                value => value / 100m);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasConversion(
                value => checked((long)(value * 100m)),
                value => value / 100m);

        modelBuilder.Entity<Order>()
            .Property(o => o.Total)
            .HasConversion(
                value => checked((long)(value * 100m)),
                value => value / 100m);

        modelBuilder.Entity<Order>()
            .Property(o => o.Subtotal)
            .HasConversion(
                value => checked((long)(value * 100m)),
                value => value / 100m);

        modelBuilder.Entity<Order>()
            .Property(o => o.Discount)
            .HasConversion(
                value => checked((long)(value * 100m)),
                value => value / 100m);
    }
}