using System.Diagnostics;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Fakes;

/// <summary>
/// Слушатели активностей для тестов. <see cref="ActivityListener.Dispose"/> не только освобождает
/// объект, но и снимает подписку с источников (в .NET нет обратного метода <c>RemoveActivityListener</c>),
/// поэтому «отписка» — это и есть <c>Dispose</c>.
/// </summary>
internal static class TestActivityListener
{
    /// <summary>Подписывается на источники и возвращает слушателя для последующей отписки.</summary>
    public static ActivityListener ListenTo(Func<ActivitySource, bool> shouldListenTo)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = shouldListenTo,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(listener);

        return listener;
    }

    /// <summary>Отписывает и освобождает слушателя: вызывается из <c>finally</c> теста.</summary>
    public static void StopListening(ActivityListener listener) => listener.Dispose();
}
