using OrderProcessing.Domain.Entities;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Domain.Tests;

public class OrderTests
{
    private static Order CreateTestOrder()
    {
        return Order.Create(
        [
            new OrderItem(1, 1, 120m)
        ]);
    }

    [Fact]
    public void Cancel_WhenPending_SetsStatusToCancelled()
    {
        var order = CreateTestOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = CreateTestOrder();
        order.Cancel();

        var exception = Assert.Throws<InvalidOperationException>(
            () => order.Cancel());

        Assert.Equal(
            "Only pending orders can be cancelled.",
            exception.Message);
    }

    [Fact]
    public void Create_WithValidItems_CalculatesSubtotal()
    {
        var order = Order.Create(
        [
            new OrderItem(1, 2, 50m)
        ]);

        Assert.Single(order.Items);
        Assert.Equal(100m, order.Subtotal);
        Assert.Equal(100m, order.Total);
    }

    [Fact]
    public void Create_WhenSubtotalIs200_HasNoDiscount()
    {
        var order = Order.Create(
        [
            new OrderItem(1, 2, 100m)
        ]);

        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(0m, order.Discount);
        Assert.Equal(200m, order.Total);
    }

    [Fact]
    public void Create_WhenSubtotalExceeds200_AppliesDiscount()
    {
        var order = Order.Create(
        [
            new OrderItem(1, 1, 120m),
            new OrderItem(2, 1, 85m)
        ]);

        Assert.Equal(205m, order.Subtotal);
        Assert.Equal(20.50m, order.Discount);
        Assert.Equal(184.50m, order.Total);
    }

    [Fact]
    public void Create_WithDuplicateProducts_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Order.Create(
            [
                new OrderItem(1, 1, 120m),
                new OrderItem(1, 2, 120m)
            ]));
    }

    [Fact]
    public void Create_WithNoItems_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => Order.Create([]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OrderItem_WithInvalidQuantity_Throws(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OrderItem(1, quantity, 100m));
    }

    [Theory]
    [InlineData("-1.00")]
    [InlineData("10.999")]
    public void OrderItem_WithInvalidPrice_Throws(
        string price)
    {
        var unitPrice = decimal.Parse(
            price,
            System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OrderItem(1, 1, unitPrice));
    }

    [Fact]
    public void Create_WithFractionalDiscount_RoundsToTwoDecimals()
    {
        var order = Order.Create(
        [
            new OrderItem(1, 1, 200.05m)
        ]);

        Assert.Equal(200.05m, order.Subtotal);
        Assert.Equal(20.01m, order.Discount);
        Assert.Equal(180.04m, order.Total);
    }
}