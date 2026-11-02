using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Infrastructure.Catalog;

public class ProductCatalog(AppDbContext db) : IProductCatalog
{
    public async Task<decimal?> GetPriceAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        return await db.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => (decimal?)product.Price)
            .FirstOrDefaultAsync(cancellationToken);
    }
}