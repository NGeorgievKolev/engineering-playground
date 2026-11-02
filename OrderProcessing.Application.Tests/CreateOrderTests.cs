using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Models;
using OrderProcessing.Application.UseCases;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.Tests;

public class CreateOrderTests
{
    [Fact]
    public async Task Execute_WithValidItems_CreatesAndSavesOrder()
    {
        var catalog = new FakeProductCatalog();
        var repository = new FakeOrderRepository();
        var useCase = new CreateOrder(catalog, repository);

        var order = await useCase.ExecuteAsync(
        [
            new CreateOrderItem(1, 1),
            new CreateOrderItem(2, 1)
        ]);

        Assert.Equal(205m, order.Subtotal);
        Assert.Equal(20.50m, order.Discount);
        Assert.Equal(184.50m, order.Total);
        Assert.Equal(2, order.Items.Count);

        Assert.Same(order, repository.AddedOrder);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task Execute_WithUnknownProduct_ThrowsWithoutSaving()
    {
        var repository = new FakeOrderRepository();
        var useCase = new CreateOrder(
            new FakeProductCatalog(),
            repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => useCase.ExecuteAsync(
            [
                new CreateOrderItem(999, 1)
            ]));

        Assert.Null(repository.AddedOrder);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Execute_WithInvalidQuantity_ThrowsWithoutSaving()
    {
        var repository = new FakeOrderRepository();
        var useCase = new CreateOrder(
            new FakeProductCatalog(),
            repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => useCase.ExecuteAsync(
            [
                new CreateOrderItem(1, 0)
            ]));

        Assert.Null(repository.AddedOrder);
        Assert.Equal(0, repository.SaveCount);
    }

    private sealed class FakeProductCatalog : IProductCatalog
    {
        public Task<decimal?> GetPriceAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            decimal? price = productId switch
            {
                1 => 120m,
                2 => 85m,
                _ => null
            };

            return Task.FromResult(price);
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? AddedOrder { get; private set; }

        public int SaveCount { get; private set; }

        public Task<Order?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Order?>(null);
        }

        public Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            AddedOrder = order;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}