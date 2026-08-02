namespace NetBench.Features.TestRun.Domain;

/// <summary>Хранилище последних отчётов прогонов (по одному на сценарий) — для экрана отчёта и сравнения.</summary>
public interface IReportStore
{
    /// <summary>Отчёты появились/обновились.</summary>
    event Action? Changed;

    void Save(TestRunReport report);

    /// <summary>Последний отчёт сценария или null, если сценарий ещё не запускали.</summary>
    TestRunReport? GetLatest(Guid scenarioId);

    /// <summary>Последние отчёты всех сценариев, новые первыми — по одному на сценарий.</summary>
    IReadOnlyList<TestRunReport> GetAll();

    /// <summary>
    /// История прогонов, новые первыми: в отличие от <see cref="GetAll"/> повторные
    /// прогоны одного сценария не схлопываются, а разовые прогоны (быстрый тест)
    /// остаются видны, хотя их сценария нет в репозитории.
    /// </summary>
    IReadOnlyList<TestRunReport> GetHistory();
}
