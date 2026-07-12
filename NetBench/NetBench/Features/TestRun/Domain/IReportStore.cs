namespace NetBench.Features.TestRun.Domain;

/// <summary>Хранилище последних отчётов прогонов (по одному на сценарий) — для экрана отчёта и сравнения.</summary>
public interface IReportStore
{
    /// <summary>Отчёты появились/обновились.</summary>
    event Action? Changed;

    void Save(TestRunReport report);

    /// <summary>Последний отчёт сценария или null, если сценарий ещё не запускали.</summary>
    TestRunReport? GetLatest(Guid scenarioId);

    /// <summary>Последние отчёты всех сценариев, новые первыми.</summary>
    IReadOnlyList<TestRunReport> GetAll();
}
