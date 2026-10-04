namespace Techodist.Basket.Application.Abstractions;

/// <summary>
/// Снимок товара из Catalog, необходимый корзине: идентификатор, название, картинка, цена.
/// Собственный контракт: Basket не ссылается на DTO каталога (границы сервисов, ADR 0001).
/// </summary>
public sealed record CatalogProduct(
    Guid Id,
    string Name,
    string? ImageUrl,
    decimal Price,
    string Currency,
    bool IsAvailable);

/// <summary>
/// Синхронный REST-клиент Catalog (лёгкий вызов через шлюз/сервис, ADR 0005):
/// нужен только для снимка товара в момент добавления в корзину.
/// </summary>
public interface ICatalogProductClient
{
    /// <summary>Возвращает товар каталога либо <c>null</c>, если товар не найден.</summary>
    Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);
}
