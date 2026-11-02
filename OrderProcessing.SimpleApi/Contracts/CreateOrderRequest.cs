namespace OrderProcessing.SimpleApi.Contracts;

public record CreateOrderRequest(
    List<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(
    int ProductId,
    int Quantity);