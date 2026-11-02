using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = [];

    public int Id { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public OrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items =>
        _items.AsReadOnly();

    public decimal Subtotal { get; private set; }

    public decimal Discount { get; private set; }

    public decimal Total { get; private set; }

    private Order()
    {
    }

    public static Order Create(
        IEnumerable<OrderItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var orderItems = items.ToList();

        if (orderItems.Count == 0)
        {
            throw new InvalidOperationException(
                "Order must contain at least one item.");
        }

        if (orderItems.Any(item => item is null))
        {
            throw new ArgumentException(
                "Order items cannot be null.",
                nameof(items));
        }

        if (orderItems
            .GroupBy(item => item.ProductId)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Duplicate products are not allowed.");
        }

        var order = new Order
        {
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        order._items.AddRange(orderItems);

        order.Subtotal = orderItems.Sum(item => item.Total);

        order.Discount = order.Subtotal > 200m
            ? decimal.Round(
                order.Subtotal * 0.10m,
                2,
                MidpointRounding.AwayFromZero)
            : 0m;

        order.Total = order.Subtotal - order.Discount;

        return order;
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending orders can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }
}