namespace OrderProcessing.Application.Models;

public record CreateOrderItem(
    int ProductId,
    int Quantity);