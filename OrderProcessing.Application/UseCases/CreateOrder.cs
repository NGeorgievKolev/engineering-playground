using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Models;
using OrderProcessing.Domain.Entities;

namespace OrderProcessing.Application.UseCases;

public class CreateOrder(
    IProductCatalog productCatalog,
    IOrderRepository orderRepository)
{
    public async Task<Order> ExecuteAsync(
        IReadOnlyCollection<CreateOrderItem> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new InvalidOperationException(
                "Order must contain at least one item.");
        }

        var orderItems = new List<OrderItem>();

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(items),
                    "Quantity must be positive.");
            }

            var price = await productCatalog.GetPriceAsync(
                item.ProductId,
                cancellationToken);

            if (price is null)
            {
                throw new KeyNotFoundException(
                    $"Product {item.ProductId} was not found.");
            }

            orderItems.Add(new OrderItem(
                item.ProductId,
                item.Quantity,
                price.Value));
        }

        var order = Order.Create(orderItems);

        await orderRepository.AddAsync(
            order,
            cancellationToken);

        await orderRepository.SaveChangesAsync(
            cancellationToken);

        return order;
    }
}