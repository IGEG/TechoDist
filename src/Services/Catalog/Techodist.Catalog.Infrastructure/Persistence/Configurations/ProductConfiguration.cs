using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Techodist.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ShortDescription).HasMaxLength(500);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.SolventType).HasMaxLength(100);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasPrecision(18, 2).IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(p => p.Slug, slug =>
        {
            slug.Property(s => s.Value).HasColumnName("Slug").HasMaxLength(200).IsRequired();
            slug.HasIndex(s => s.Value).IsUnique();
        });

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.SolventType);
        builder.HasIndex(p => p.Status);
    }
}
