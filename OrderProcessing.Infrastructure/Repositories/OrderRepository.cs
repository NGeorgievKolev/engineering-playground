using Microsoft.EntityFrameworkCore;
using OrderProcessing.Application.Abstractions;
using OrderProcessing.Domain.Entities;
using OrderProcessing.Infrastructure.Persistence;

namespace OrderProcessing.Infrastructure.Repositories;

public class OrderRepository(AppDbContext db)
    : IOrderRepository
{
    public async Task AddAsync(
    Order order,
    CancellationToken cancellationToken = default)
    {
        await db.Orders.AddAsync(
            order,
            cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await db.Orders
            .FirstOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}