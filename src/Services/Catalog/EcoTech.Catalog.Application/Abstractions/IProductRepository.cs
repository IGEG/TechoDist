using EcoTech.Catalog.Application.Models;
using EcoTech.Catalog.Domain.Entities;

namespace EcoTech.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    void Update(Product product);

    void Remove(Product product);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
