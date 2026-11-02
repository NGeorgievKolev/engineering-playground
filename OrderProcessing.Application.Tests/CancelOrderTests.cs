using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Results;
using OrderProcessing.Application.UseCases;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.Tests;

public class CancelOrderTests
{
    private static Order CreateTestOrder()
    {
        return Order.Create(
        [
            new OrderItem(1, 1, 120m)
        ]);
    }

    [Fact]
    public async Task Execute_WhenOrderExists_CancelsAndSavesOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        var repository = new FakeOrderRepository(order);
        var useCase = new CancelOrder(repository);

        // Act
        var result = await useCase.ExecuteAsync(1);

        // Assert
        Assert.Equal(CancelOrderResult.Success, result);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Execute_WhenOrderDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var repository = new FakeOrderRepository(null);
        var useCase = new CancelOrder(repository);

        // Act
        var result = await useCase.ExecuteAsync(999);

        // Assert
        Assert.Equal(CancelOrderResult.NotFound, result);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Execute_WhenOrderAlreadyCancelled_ReturnsAlreadyCancelled()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        var repository = new FakeOrderRepository(order);
        var useCase = new CancelOrder(repository);

        // Act
        var result = await useCase.ExecuteAsync(1);

        // Assert
        Assert.Equal(
            CancelOrderResult.AlreadyCancelled,
            result);

        Assert.Equal(0, repository.SaveCount);
    }

    private sealed class FakeOrderRepository(
        Order? order) : IOrderRepository
    {
        public int SaveCount { get; private set; }

        public Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "AddAsync is not used by CancelOrder tests.");
        }

        public Task<Order?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(order);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}