using EcoTech.Catalog.Domain.Entities;

namespace EcoTech.Catalog.Application.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(bool onlyPublished = false, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Update(Category category);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
