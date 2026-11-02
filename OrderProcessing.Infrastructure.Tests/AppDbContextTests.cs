using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Infrastructure.Tests;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveAndLoadOrder_PreservesOrderData()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);

        await db.Database.EnsureCreatedAsync();

        var order = Order.Create(
        [
            new OrderItem(1, 1, 120m),
            new OrderItem(2, 1, 85m)
        ]);

        // Act - Save
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var orderId = order.Id;

        // Stop tracking the original objects
        db.ChangeTracker.Clear();

        // Act - Load
        var savedOrder = await db.Orders
            .Include(o => o.Items)
            .SingleAsync(o => o.Id == orderId);

        // Assert
        Assert.Equal(OrderStatus.Pending, savedOrder.Status);

        Assert.Equal(2, savedOrder.Items.Count);

        Assert.Equal(205m, savedOrder.Subtotal);
        Assert.Equal(20.50m, savedOrder.Discount);
        Assert.Equal(184.50m, savedOrder.Total);

        Assert.Contains(savedOrder.Items,
            item => item.ProductId == 1
                && item.UnitPrice == 120m);

        Assert.Contains(savedOrder.Items,
            item => item.ProductId == 2
                && item.UnitPrice == 85m);
    }
}