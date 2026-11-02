using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;
using OrderProcessing.Infrastructure.Persistence;
using OrderProcessing.Infrastructure.Repositories;

namespace OrderProcessing.Infrastructure.Tests;

public class OrderRepositoryTests
{
    [Fact]
    public async Task GetByIdAndSaveChanges_PersistsCancellation()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        int orderId;

        await using (var setupDb = new AppDbContext(options))
        {
            await setupDb.Database.EnsureCreatedAsync();

            var order = Order.Create(
            [
                new OrderItem(1, 1, 120m)
            ]);

            setupDb.Orders.Add(order);
            await setupDb.SaveChangesAsync();

            orderId = order.Id;
        }

        // Act
        await using (var db = new AppDbContext(options))
        {
            var repository = new OrderRepository(db);

            var order = await repository.GetByIdAsync(orderId);

            Assert.NotNull(order);

            order.Cancel();

            await repository.SaveChangesAsync();
        }

        // Assert - load using a fresh DbContext
        await using (var verificationDb = new AppDbContext(options))
        {
            var savedOrder = await verificationDb.Orders
                .SingleAsync(o => o.Id == orderId);

            Assert.Equal(
                OrderStatus.Cancelled,
                savedOrder.Status);
        }
    }
}