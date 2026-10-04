using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Dtos;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>Подменный клиент Basket: отдаёт заранее заданный снимок корзины.</summary>
internal sealed class FakeBasketClient(BasketSnapshot? snapshot = null) : IBasketClient
{
    public int GetCalls { get; private set; }

    public int ClearCalls { get; private set; }

    public Guid? ClearedBasketId { get; private set; }

    /// <summary>
    /// Исключение, которое бросит очистка корзины. Нужен, чтобы проверить, что недоступность
    /// Basket после успешного оформления не превращает ответ гостю в ошибку.
    /// </summary>
    public Exception? ClearFailure { get; set; }

    public Task<BasketSnapshot?> GetBasketAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        GetCalls++;

        return Task.FromResult(snapshot);
    }

    public Task ClearBasketAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        ClearCalls++;
        ClearedBasketId = basketId;

        return ClearFailure is null ? Task.CompletedTask : Task.FromException(ClearFailure);
    }
}
