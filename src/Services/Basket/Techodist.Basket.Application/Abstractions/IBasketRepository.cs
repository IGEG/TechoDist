using Techodist.Basket.Domain.Entities;

namespace Techodist.Basket.Application.Abstractions;

/// <summary>
/// Хранилище корзин (Redis, ADR 0005). Агрегат целиком сериализуется по ключу <c>basket:{basketId}</c>,
/// список корзин не поддерживается — поиск всегда по идентификатору из cookie.
/// </summary>
public interface IBasketRepository
{
    Task<ShoppingBasket?> GetAsync(Guid basketId, CancellationToken cancellationToken = default);

    Task SaveAsync(ShoppingBasket basket, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid basketId, CancellationToken cancellationToken = default);
}
