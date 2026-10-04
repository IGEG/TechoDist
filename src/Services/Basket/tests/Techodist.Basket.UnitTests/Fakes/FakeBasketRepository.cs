using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Domain.Entities;

namespace Techodist.Basket.UnitTests.Fakes;

/// <summary>In-memory хранилище корзин для тестов: считает сохранения и удаления.</summary>
internal sealed class FakeBasketRepository : IBasketRepository
{
    private readonly Dictionary<Guid, ShoppingBasket> _store = [];
    private readonly List<Guid> _deleted = [];

    public int SaveCalls { get; private set; }

    public IReadOnlyList<Guid> DeletedBasketIds => _deleted;

    public void Seed(ShoppingBasket basket) => _store[basket.Id] = basket;

    public bool Contains(Guid basketId) => _store.ContainsKey(basketId);

    public Task<ShoppingBasket?> GetAsync(Guid basketId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(basketId, out var basket) ? basket : null);

    public Task SaveAsync(ShoppingBasket basket, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        _store[basket.Id] = basket;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        _deleted.Add(basketId);
        _store.Remove(basketId);

        return Task.CompletedTask;
    }
}
