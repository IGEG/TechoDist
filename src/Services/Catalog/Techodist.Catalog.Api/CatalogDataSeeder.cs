using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.ValueObjects;
using Techodist.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Techodist.Catalog.Api;

/// <summary>
/// Применяет миграции и заполняет каталог демонстрационными данными — оборудование
/// <b>Techodist (TD)</b> для регенерации растворителей. Номер модели отражает объём бака:
/// TD20 (20 л), TD60 (60 л), TD120 (120 л); вакуумное исполнение — TDV40.
/// </summary>
public static class CatalogDataSeeder
{
    /// <summary>Торговая марка оборудования.</summary>
    public const string Brand = "Techodist";

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

        var categories = new List<Category>
        {
            Category.Create(
                "Стандартные дистилляторы",
                "Установки для регенерации растворителей в стандартном исполнении.",
                sortOrder: 1),
            Category.Create(
                "Вакуумные установки",
                "Регенерация под вакуумом при пониженной температуре.",
                sortOrder: 2),
            Category.Create(
                "Комплектующие",
                "Насосы, ёмкости и расходные материалы.",
                sortOrder: 3),
        };

        await db.Categories.AddRangeAsync(categories);
        await db.SaveChangesAsync();

        var standard = categories[0];
        var vacuum = categories[1];
        var parts = categories[2];

        var products = new List<Product>
        {
            Product.Create(
                $"Установка регенерации растворителей {Brand} TD20",
                standard.Id,
                Money.Rub(320_000m),
                "Компактная установка на 20 л — для небольших участков и лабораторий.",
                "Настольная установка для регенерации органических растворителей методом дистилляции.",
                "Универсальный",
                20),
            Product.Create(
                $"Установка регенерации растворителей {Brand} TD60",
                standard.Id,
                Money.Rub(485_000m),
                "Производительность 60 л/смену, взрывозащищённое исполнение.",
                "Установка предназначена для регенерации органических растворителей методом дистилляции.",
                "Универсальный",
                60),
            Product.Create(
                $"Установка регенерации растворителей {Brand} TD120",
                standard.Id,
                Money.Rub(720_000m),
                "Производительность 120 л/смену, автоматический контроль процесса.",
                "Установка большой производительности для регенерации растворителей.",
                "Универсальный",
                120),
            Product.Create(
                $"Вакуумная установка {Brand} TDV40",
                vacuum.Id,
                Money.Rub(560_000m),
                "Регенерация при низкой температуре под вакуумом.",
                "Вакуумная установка для термочувствительных растворителей.",
                "Термочувствительный",
                40),
            Product.Create(
                $"Насос диафрагменный {Brand} TD-P40",
                parts.Id,
                Money.Rub(38_000m),
                "Химически стойкий диафрагменный насос производительностью 40 л/ч.",
                "Насос для перекачки агрессивных растворителей.",
                "Универсальный"),
        };

        AddDetails(
            products[0],
            "TD20",
            [("Объём бака", "20 л"), ("Мощность", "3,5 кВт"), ("Производительность", "20 л/смену")]);

        AddDetails(
            products[1],
            "TD60",
            [("Объём бака", "60 л"), ("Мощность", "6 кВт"), ("Производительность", "60 л/смену")]);

        AddDetails(
            products[2],
            "TD120",
            [("Объём бака", "120 л"), ("Мощность", "9 кВт"), ("Производительность", "120 л/смену")]);

        AddDetails(
            products[3],
            "TDV40",
            [("Объём бака", "40 л"), ("Остаточное давление", "50 мбар"), ("Температура процесса", "40–60 °C")]);

        AddDetails(
            products[4],
            "TD-P40",
            [("Производительность", "40 л/ч"), ("Материал корпуса", "Полипропилен"), ("Диаметр патрубка", "1\"")]);

        await db.Products.AddRangeAsync(products);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Каталог заполнен демо-данными: {CategoryCount} категорий, {ProductCount} товаров.",
            categories.Count,
            products.Count);
    }

    /// <summary>Публикует товар и добавляет модель, характеристики и главное изображение.</summary>
    private static void AddDetails(Product product, string model, (string Name, string Value)[] specifications)
    {
        product.Publish();
        product.AddSpecification("Модель", model);

        foreach (var (name, value) in specifications)
        {
            product.AddSpecification(name, value);
        }

        product.AddImage($"/images/{Slug.FromName($"{Brand}-{model}").Value}.jpg", $"{Brand} {model}", isMain: true);
    }
}
