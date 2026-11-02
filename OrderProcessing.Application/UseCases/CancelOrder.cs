using OrderProcessing.Application.Abstractions;
using OrderProcessing.Application.Results;
using OrderProcessing.Domain.Enums;

namespace OrderProcessing.Application.UseCases;

public class CancelOrder(IOrderRepository repository)
{
    public async Task<CancelOrderResult> ExecuteAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await repository.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return CancelOrderResult.NotFound;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return CancelOrderResult.AlreadyCancelled;
        }

        order.Cancel();

        await repository.SaveChangesAsync(cancellationToken);

        return CancelOrderResult.Success;
    }
}