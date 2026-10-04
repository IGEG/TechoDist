using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techodist.Order.Domain.Entities;

namespace Techodist.Order.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<OrderAggregate>
{
    public void Configure(EntityTypeBuilder<OrderAggregate> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.OwnsOne(order => order.Number, number =>
        {
            number.Property(value => value.Value)
                .HasColumnName("Number")
                .HasMaxLength(32)
                .IsRequired();

            number.HasIndex(value => value.Value).IsUnique();
        });

        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(order => order.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(order => order.CustomerEmail).IsRequired().HasMaxLength(256);
        builder.Property(order => order.CustomerPhone).HasMaxLength(50);
        builder.Property(order => order.Comment).HasMaxLength(2000);
        builder.Property(order => order.ManagerComment).HasMaxLength(2000);
        builder.Property(order => order.PreferredChannel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(order => order.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Позиции — часть агрегата: заявку нельзя удалить, оставив «сирот», и читаем мы её всегда с позициями.
        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CustomerEmail);
        builder.HasIndex(order => order.CreatedAt);
    }
}
