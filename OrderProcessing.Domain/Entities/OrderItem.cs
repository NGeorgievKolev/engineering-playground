namespace OrderProcessing.Domain.Entities;

public class OrderItem
{
    public int Id { get; private set; }

    public int OrderId { get; private set; }

    public int ProductId { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Total => UnitPrice * Quantity;

    private OrderItem()
    {
    }

    public OrderItem(
        int productId,
        int quantity,
        decimal unitPrice)
    {
        if (productId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        if (unitPrice < 0 || decimal.Round(unitPrice, 2) != unitPrice)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice));

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}