namespace OrderProcessing.SimpleApi.Models;

public class Order
{
    public int Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}