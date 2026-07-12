using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.TestRun.Data;

/// <summary>Отчёты живут в памяти на время сессии; персистентность пока не нужна.</summary>
public sealed class InMemoryReportStore : IReportStore
{
    private readonly Dictionary<Guid, TestRunReport> _reports = [];
    private readonly Lock _lock = new();

    public event Action? Changed;

    public void Save(TestRunReport report)
    {
        lock (_lock)
        {
            _reports[report.Scenario.Id] = report;
        }

        Changed?.Invoke();
    }

    public TestRunReport? GetLatest(Guid scenarioId)
    {
        lock (_lock)
        {
            return _reports.GetValueOrDefault(scenarioId);
        }
    }

    public IReadOnlyList<TestRunReport> GetAll()
    {
        lock (_lock)
        {
            return [.. _reports.Values.OrderByDescending(r => r.FinishedAt)];
        }
    }
}
