using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Techodist.Catalog.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.OwnsOne(c => c.Slug, slug =>
        {
            slug.Property(s => s.Value).HasColumnName("Slug").HasMaxLength(200).IsRequired();
            slug.HasIndex(s => s.Value).IsUnique();
        });

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.ParentId);
    }
}
