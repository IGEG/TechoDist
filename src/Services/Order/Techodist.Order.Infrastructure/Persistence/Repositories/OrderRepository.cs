using Microsoft.EntityFrameworkCore;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Models;
using Techodist.Order.Domain.ValueObjects;
using Techodist.Order.Infrastructure.Persistence;

namespace Techodist.Order.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(OrderDbContext db) : IOrderRepository
{
    public Task<OrderAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task<OrderAggregate?> GetByNumberAsync(OrderNumber number, CancellationToken cancellationToken = default)
        => db.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Number.Value == number.Value, cancellationToken);

    public async Task<(IReadOnlyList<OrderAggregate> Items, int TotalCount)> ListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Список читается без позиций: админке нужны номер, клиент и сумма, а позиции — в карточке.
        var query = db.Orders.AsNoTracking();

        if (filter.Status is { } status)
        {
            query = query.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";

            query = query.Where(order =>
                EF.Functions.ILike(order.Number.Value, pattern) ||
                EF.Functions.ILike(order.CustomerName, pattern) ||
                EF.Functions.ILike(order.CustomerEmail, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .Skip((filter.NormalizedPage - 1) * filter.NormalizedPageSize)
            .Take(filter.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(OrderAggregate order, CancellationToken cancellationToken = default)
        => await db.Orders.AddAsync(order, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
