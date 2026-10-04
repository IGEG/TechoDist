using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Application.Abstractions;

/// <summary>
/// Генератор читаемого номера заявки. Реализация опирается на последовательность PostgreSQL,
/// поэтому номер уникален даже при одновременном оформлении несколькими гостями.
/// </summary>
public interface IOrderNumberGenerator
{
    Task<OrderNumber> NextAsync(DateOnly date, CancellationToken cancellationToken = default);
}
