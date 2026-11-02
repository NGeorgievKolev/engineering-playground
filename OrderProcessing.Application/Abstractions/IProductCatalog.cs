namespace OrderProcessing.Application.Abstractions;

public interface IProductCatalog
{
    Task<decimal?> GetPriceAsync(
        int productId,
        CancellationToken cancellationToken = default);
}