using MassTransit;
using Microsoft.EntityFrameworkCore;
using Techodist.Order.Domain.Entities;

namespace Techodist.Order.Infrastructure.Persistence;

/// <summary>
/// БД заявок. Кроме доменных таблиц здесь живут таблицы transactional outbox MassTransit:
/// заявка и событие для Notification записываются одной транзакцией, поэтому «заказ без события»
/// и «событие без заказа» невозможны (ADR 0003).
/// </summary>
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    /// <summary>Последовательность PostgreSQL для сквозного номера заявки.</summary>
    public const string OrderNumberSequenceName = "order_number_seq";

    public DbSet<OrderAggregate> Orders => Set<OrderAggregate>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);

        modelBuilder.HasSequence<long>(OrderNumberSequenceName)
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
