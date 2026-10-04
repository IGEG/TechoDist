using Microsoft.EntityFrameworkCore;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Domain.ValueObjects;
using Techodist.Order.Infrastructure.Persistence;

namespace Techodist.Order.Infrastructure.Numbering;

/// <summary>
/// Номер заявки собирается из даты и значения последовательности PostgreSQL. Последовательность
/// атомарна на уровне БД: два гостя, оформившие заявку в одну секунду, получат разные номера
/// без блокировок и без «счётчика в памяти», который теряется при рестарте.
/// </summary>
internal sealed class OrderNumberGenerator(OrderDbContext db) : IOrderNumberGenerator
{
    public async Task<OrderNumber> NextAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var sequence = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{OrderDbContext.OrderNumberSequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        return OrderNumber.Create(date, sequence);
    }
}
