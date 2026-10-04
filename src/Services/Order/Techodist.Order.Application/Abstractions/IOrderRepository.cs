using Techodist.Order.Application.Models;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Application.Abstractions;

/// <summary>Хранилище заявок (PostgreSQL, database-per-service — ADR 0002).</summary>
public interface IOrderRepository
{
    Task<OrderAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Поиск по читаемому номеру: клиент называет его в письме/по телефону.</summary>
    Task<OrderAggregate?> GetByNumberAsync(OrderNumber number, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrderAggregate> Items, int TotalCount)> ListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrderAggregate order, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
