using Mapster;
using Techodist.Order.Application.Common;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>
/// Регистрирует правила маппинга так же, как это делает <c>AddOrderApplication</c>, чтобы
/// тесты проверяли реальную конфигурацию Mapster, а не «случайный» маппинг по соглашению.
/// </summary>
internal static class MappingFixture
{
    static MappingFixture() => OrderMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

    public static void EnsureRegistered()
    {
        // Вызов нужен только для срабатывания статического конструктора.
    }
}
