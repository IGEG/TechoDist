using EcoTech.Catalog.Domain.Entities;
using EcoTech.Catalog.Domain.ValueObjects;
using EcoTech.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcoTech.Catalog.Api;

/// <summary>
/// Применяет миграции и заполняет каталог демонстрационными данными
/// (оборудование для регенерации растворителей).
/// </summary>
public static class CatalogDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        logger.LogInformation("Применение миграций Catalog...");
        await db.Database.MigrateAsync();

        if (await db.Products.AnyAsync())
        {
            logger.LogInformation("Каталог уже содержит данные — сидирование пропущено.");
            return;
        }

        var standard = Category.Create(
            "Стандартные дистилляторы",
            "Установки для регенерации растворителей в стандартном исполнении.",
            sortOrder: 1);

        var vacuum = Category.Create(
            "Вакуумные установки",
            "Регенерация под вакуумом при пониженной температуре.",
            sortOrder: 2);

        var parts = Category.Create(
            "Комплектующие",
            "Насосы, ёмкости и расходные материалы.",
            sortOrder: 3);

        await db.Categories.AddRangeAsync(standard, vacuum, parts);
        await db.SaveChangesAsync();

        var products = new List<Product>
        {
            Product.Create(
                "Установка регенерации растворителей ЭКОТЕХ ET-60",
                standard.Id,
                Money.Rub(485_000m),
                "Производительность 60 л/смену, взрывозащищённое исполнение.",
                "Установка предназначена для регенерации органических растворителей методом дистилляции.",
                "Универсальный",
                60),
            Product.Create(
                "Установка регенерации растворителей ЭКОТЕХ ET-120",
                standard.Id,
                Money.Rub(720_000m),
                "Производительность 120 л/смену, автоматический контроль процесса.",
                "Установка большой производительности для регенерации растворителей.",
                "Универсальный",
                120),
            Product.Create(
                "Вакуумная установка ЭКОТЕХ VAC-40",
                vacuum.Id,
                Money.Rub(560_000m),
                "Регенерация при низкой температуре под вакуумом.",
                "Вакуумная установка для термочувствительных растворителей.",
                "Термочувствительный",
                40),
            Product.Create(
                "Насос диафрагменный для перекачки растворителей",
                parts.Id,
                Money.Rub(38_000m),
                "Химически стойкий диафрагменный насос.",
                "Насос для перекачки агрессивных растворителей.",
                "Универсальный"),
        };

        foreach (var product in products)
        {
            product.Publish();
        }

        products[0].AddSpecification("Объём бака", "60 л");
        products[0].AddSpecification("Мощность", "6 кВт");
        products[0].AddImage("/images/placeholder-et60.jpg", "Установка ЭКОТЕХ ET-60", isMain: true);

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Каталог заполнен демо-данными: {CategoryCount} категорий, {ProductCount} товаров.",
            3,
            products.Count);
    }
}
