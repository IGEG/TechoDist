using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Techodist.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(CatalogDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> ListAsync(bool onlyPublished = false, CancellationToken cancellationToken = default)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();

        if (onlyPublished)
        {
            query = query.Where(c => c.IsPublished);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => db.Categories.AnyAsync(
            c => c.Slug.Value == slug && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        => db.Categories.AnyAsync(c => c.Name == name, cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => await db.Categories.AddAsync(category, cancellationToken);

    public void Update(Category category) => db.Categories.Update(category);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
