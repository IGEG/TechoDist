using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Models;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>
/// In-memory хранилище заявок: повторяет фильтрацию/сортировку EF-репозитория, но без БД,
/// поэтому юнит-тесты обработчиков не требуют PostgreSQL.
/// </summary>
internal sealed class FakeOrderRepository(CallLog? log = null) : IOrderRepository
{
    private readonly List<OrderAggregate> _orders = [];

    public int SaveCalls { get; private set; }

    public int GetByIdCalls { get; private set; }

    public int GetByNumberCalls { get; private set; }

    public int ListCalls { get; private set; }

    public IReadOnlyList<OrderAggregate> AddedOrders => _orders;

    public void Seed(OrderAggregate order) => _orders.Add(order);

    public Task<OrderAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        GetByIdCalls++;

        return Task.FromResult(_orders.FirstOrDefault(order => order.Id == id));
    }

    public Task<OrderAggregate?> GetByNumberAsync(OrderNumber number, CancellationToken cancellationToken = default)
    {
        GetByNumberCalls++;

        return Task.FromResult(_orders.FirstOrDefault(order => order.Number.Value == number.Value));
    }

    public Task<(IReadOnlyList<OrderAggregate> Items, int TotalCount)> ListAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ListCalls++;

        IEnumerable<OrderAggregate> query = _orders;

        if (filter.Status is { } status)
        {
            query = query.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(order =>
                order.Number.Value.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                order.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                order.CustomerEmail.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderByDescending(order => order.CreatedAt).ToList();
        var totalCount = ordered.Count;

        var items = ordered
            .Skip((filter.NormalizedPage - 1) * filter.NormalizedPageSize)
            .Take(filter.NormalizedPageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<OrderAggregate>)items, totalCount));
    }

    public Task AddAsync(OrderAggregate order, CancellationToken cancellationToken = default)
    {
        _orders.Add(order);

        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        log?.Add("save");

        return Task.FromResult(1);
    }
}
