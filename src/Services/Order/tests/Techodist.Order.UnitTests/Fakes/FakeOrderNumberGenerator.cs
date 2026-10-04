using Techodist.Order.Application.Abstractions;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>
/// Подменный генератор номера: в проде номер выдаёт последовательность PostgreSQL,
/// в тестах достаточно счётчика — важно лишь, что номер уникален и читаем.
/// </summary>
internal sealed class FakeOrderNumberGenerator(long startFrom = 1) : IOrderNumberGenerator
{
    private long _sequence = startFrom - 1;

    public int Calls { get; private set; }

    public DateOnly? LastDate { get; private set; }

    public Task<OrderNumber> NextAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastDate = date;

        return Task.FromResult(OrderNumber.Create(date, ++_sequence));
    }
}
