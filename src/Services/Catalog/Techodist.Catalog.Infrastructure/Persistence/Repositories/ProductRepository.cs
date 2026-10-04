using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Models;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.Enums;
using Techodist.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Techodist.Catalog.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(CatalogDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Products
            .Include(p => p.Images)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => db.Products
            .Include(p => p.Images)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Slug.Value == slug, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => db.Products.AnyAsync(
            p => p.Slug.Value == slug && (excludeId == null || p.Id != excludeId),
            cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking().AsQueryable();

        if (filter.OnlyPublished)
        {
            query = query.Where(p => p.Status == ProductStatus.Published);
        }
        else if (filter.Status is { } status)
        {
            // Админский отбор (черновики/архив) возможен только когда витринный фильтр не активен.
            query = query.Where(p => p.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SolventType))
        {
            var solvent = filter.SolventType.Trim();
            query = query.Where(p => p.SolventType == solvent);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern) ||
                (p.ShortDescription != null && EF.Functions.ILike(p.ShortDescription, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = filter.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price.Amount),
            "price_desc" => query.OrderByDescending(p => p.Price.Amount),
            "name_desc" => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };

        var items = await ordered
            .Include(p => p.Images)
            .Skip((filter.NormalizedPage - 1) * filter.NormalizedPageSize)
            .Take(filter.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await db.Products.AddAsync(product, cancellationToken);

    public void Update(Product product) => db.Products.Update(product);

    public void Remove(Product product) => db.Products.Remove(product);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
