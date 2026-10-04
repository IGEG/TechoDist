using Serilog.Core;
using Serilog.Events;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Fakes;

/// <summary>
/// Фабрика свойств Serilog для тестов энричеров: значения складываются в
/// <see cref="ScalarValue"/>, как это делает боевой <c>LogEventPropertyFactory</c>.
/// </summary>
internal sealed class TestPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        => new(name, new ScalarValue(value));
}
