using Xunit;

// Инструменты телеметрии (ActivitySource/Meter в TechodistDiagnostics) — статические и живут
// на весь процесс, поэтому тесты наблюдают их через слушателей и не должны выполняться
// параллельно: измерения одного теста иначе попадут в слушатель другого.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
