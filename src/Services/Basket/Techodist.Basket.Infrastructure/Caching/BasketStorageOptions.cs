namespace Techodist.Basket.Infrastructure.Caching;

/// <summary>Настройки хранения корзин в Redis (секция <c>Basket:Storage</c>).</summary>
public sealed class BasketStorageOptions
{
    public const string SectionName = "Basket:Storage";

    /// <summary>Срок жизни корзины. По ADR 0005 — примерно 30 дней.</summary>
    public const int DefaultTtlDays = 30;

    /// <summary>Префикс ключа Redis: <c>basket:{basketId}</c>.</summary>
    public string KeyPrefix { get; set; } = "basket:";

    public int TtlDays { get; set; } = DefaultTtlDays;

    public TimeSpan Ttl => TimeSpan.FromDays(TtlDays <= 0 ? DefaultTtlDays : TtlDays);
}
