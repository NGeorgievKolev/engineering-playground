using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Infrastructure.Persistence.Models;

namespace OrderProcessing.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.Status)
                .HasConversion<int>();

            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Navigation(o => o.Items)
                .HasField("_items")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Property(o => o.Subtotal)
                .HasConversion(
                    value => checked((long)(value * 100m)),
                    value => value / 100m);

            entity.Property(o => o.Discount)
                .HasConversion(
                    value => checked((long)(value * 100m)),
                    value => value / 100m);

            entity.Property(o => o.Total)
                .HasConversion(
                    value => checked((long)(value * 100m)),
                    value => value / 100m);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Price)
                .HasConversion(
                    value => checked((long)(value * 100m)),
                    value => value / 100m);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Ignore(i => i.Total);

            entity.Property(i => i.UnitPrice)
                .HasConversion(
                    value => checked((long)(value * 100m)),
                    value => value / 100m);
        });
    }
}