using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techodist.Order.Domain.Entities;

namespace Techodist.Order.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.ProductName).IsRequired().HasMaxLength(OrderAggregate.MaxItemNameLength);
        builder.Property(item => item.ImageUrl).HasMaxLength(500);
        builder.Property(item => item.Quantity).IsRequired();

        builder.OwnsOne(item => item.UnitPrice, price =>
        {
            price.Property(money => money.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired();
            price.Property(money => money.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.HasIndex(item => item.ProductId);
    }
}
