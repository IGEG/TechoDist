namespace Techodist.Order.UnitTests.Fakes;

/// <summary>
/// Общий журнал действий подменных зависимостей: позволяет проверить порядок шагов обработчика
/// (например, событие публикуется до сохранения — требование bus-outbox, ADR 0003).
/// </summary>
internal sealed class CallLog
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string entry) => _entries.Add(entry);
}
