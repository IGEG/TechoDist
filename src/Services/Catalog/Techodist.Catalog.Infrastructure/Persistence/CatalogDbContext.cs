using Techodist.Catalog.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Techodist.Catalog.Infrastructure.Persistence;

/// <summary>
/// БД каталога. Кроме доменных таблиц здесь живут таблицы transactional outbox MassTransit:
/// изменение товара и событие для Search записываются одной транзакцией, поэтому «товар без
/// события» (и наоборот) невозможен, а индекс поиска не расходится с каталогом (ADR 0003, 0009).
/// </summary>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
