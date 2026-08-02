using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.TestRun.Data;

/// <summary>Отчёты живут в памяти на время сессии; персистентность пока не нужна.</summary>
public sealed class InMemoryReportStore : IReportStore
{
    /// <summary>Глубина истории: список сессий дальше не листают, а отчёты держат таймлайн.</summary>
    public const int HistoryLimit = 50;

    private readonly Dictionary<Guid, TestRunReport> _latest = [];

    // Новые первыми — так же отдаём наружу, лишних сортировок на UI не нужно.
    private readonly List<TestRunReport> _history = [];
    private readonly Lock _lock = new();

    public event Action? Changed;

    public void Save(TestRunReport report)
    {
        lock (_lock)
        {
            _latest[report.Scenario.Id] = report;

            _history.Insert(0, report);
            if (_history.Count > HistoryLimit)
                _history.RemoveRange(HistoryLimit, _history.Count - HistoryLimit);
        }

        Changed?.Invoke();
    }

    public TestRunReport? GetLatest(Guid scenarioId)
    {
        lock (_lock)
        {
            return _latest.GetValueOrDefault(scenarioId);
        }
    }

    public IReadOnlyList<TestRunReport> GetAll()
    {
        lock (_lock)
        {
            return [.. _latest.Values.OrderByDescending(r => r.FinishedAt)];
        }
    }

    public IReadOnlyList<TestRunReport> GetHistory()
    {
        lock (_lock)
        {
            return [.. _history];
        }
    }
}
