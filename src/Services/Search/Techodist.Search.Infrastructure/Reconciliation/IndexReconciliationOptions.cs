namespace Techodist.Search.Infrastructure.Reconciliation;

/// <summary>Настройки сверки индекса с каталогом (секция конфигурации "Search:Reindex").</summary>
public sealed class IndexReconciliationOptions
{
    public const string SectionName = "Search:Reindex";

    /// <summary>Выполнять сверку по расписанию (стартовая сверка выполняется всегда).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Интервал сверки, минут. 0 — сверять только при старте сервиса.</summary>
    public int IntervalMinutes { get; set; } = 30;
}
