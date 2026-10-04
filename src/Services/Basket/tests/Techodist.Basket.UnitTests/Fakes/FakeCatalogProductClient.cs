using Techodist.Basket.Application.Abstractions;

namespace Techodist.Basket.UnitTests.Fakes;

/// <summary>Подменный клиент Catalog: возвращает заранее заданные товары.</summary>
internal sealed class FakeCatalogProductClient : ICatalogProductClient
{
    private readonly Dictionary<Guid, CatalogProduct> _products = [];

    public int Calls { get; private set; }

    public void Add(CatalogProduct product) => _products[product.Id] = product;

    public Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(_products.TryGetValue(productId, out var product) ? product : null);
    }
}
