namespace Techodist.Notification.UnitTests.Fakes;

/// <summary>Управляемые часы: тесты срока хранения отметок двигают время вручную.</summary>
internal sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public void Advance(TimeSpan delta) => UtcNow += delta;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
