using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Search.Application.Models;

namespace Techodist.Search.UnitTests.Fakes;

/// <summary>Тестовые данные поиска: одна карточка каталога TD60 и её события/снимки.</summary>
internal static class SearchTestData
{
    public static readonly Guid ProductId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public static readonly Guid CategoryId = Guid.Parse("99999999-8888-7777-6666-555555555555");

    public const string ProductName = "Установка TD60";

    public const string ProductSlug = "ustanovka-td60";

    public const string ProductDescription = "Регенерация 60 л растворителя за смену.";

    public const string SolventType = "Универсальный";

    public const int VolumeLiters = 60;

    public const string MainImageUrl = "/images/products/td60.png";

    public const string CategoryName = "Установки регенерации";

    public const decimal Price = 289000m;

    public static readonly DateTimeOffset OccurredAt = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Событие «товар создан/обновлён и опубликован» — обычный путь наполнения индекса.</summary>
    public static ProductChangedIntegrationEvent Published(ProductChangeType changeType = ProductChangeType.Created) => new()
    {
        ProductId = ProductId,
        Name = ProductName,
        Slug = ProductSlug,
        ShortDescription = ProductDescription,
        Price = Price,
        Currency = "RUB",
        SolventType = SolventType,
        VolumeLiters = VolumeLiters,
        MainImageUrl = MainImageUrl,
        CategoryId = CategoryId,
        CategoryName = CategoryName,
        IsPublished = true,
        ChangeType = changeType,
        OccurredAt = OccurredAt,
    };

    /// <summary>Событие по черновику: в поиске такого товара быть не должно.</summary>
    public static ProductChangedIntegrationEvent Draft()
        => Published(ProductChangeType.Updated) with { IsPublished = false };

    /// <summary>Событие удаления товара из каталога.</summary>
    public static ProductChangedIntegrationEvent Deleted()
        => Published(ProductChangeType.Deleted) with { IsPublished = false };

    /// <summary>Тот же товар, но прочитанный публичным API каталога при реконсиляции.</summary>
    public static CatalogProductSnapshot Snapshot() => new(
        ProductId,
        ProductName,
        ProductSlug,
        ProductDescription,
        Price,
        "RUB",
        CategoryId,
        SolventType,
        VolumeLiters,
        MainImageUrl);
}
