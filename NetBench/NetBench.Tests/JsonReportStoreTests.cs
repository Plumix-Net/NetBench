using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

public sealed class JsonReportStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "netbench-tests", Guid.NewGuid().ToString("N"));

    private string HistoryPath => Path.Combine(_directory, "history.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static TestRunReport Report(LoadScenario scenario, DateTime finishedAt) => new()
    {
        Scenario = scenario,
        StartedAt = finishedAt.AddSeconds(-30),
        FinishedAt = finishedAt,
        Summary = new TestRunStats { TotalRequests = 100, ErrorCount = 3, LatencyP95Ms = 42 },
        StatusCodes = [new StatusCodeBucket { StatusCode = 200, Count = 97 }],
        Timeline = [new TestRunStats { TotalRequests = 50 }],
    };

    [Fact]
    public void RestoresHistoryAfterRestartNewestFirst()
    {
        var scenario = new LoadScenario { Name = "s", Target = "https://example.com" };
        var quick = new LoadScenario { Name = "quick" };
        var store = new JsonReportStore(HistoryPath);
        store.Save(Report(scenario, DateTime.UtcNow.AddMinutes(-5)));
        store.Save(Report(quick, DateTime.UtcNow.AddMinutes(-3)));
        store.Save(Report(scenario, DateTime.UtcNow));

        var restored = new JsonReportStore(HistoryPath);

        Assert.Equal(["s", "quick", "s"], restored.GetHistory().Select(r => r.Scenario.Name).ToArray());
        Assert.Equal(2, restored.GetAll().Count);
        var latest = restored.GetLatest(scenario.Id);
        Assert.NotNull(latest);
        Assert.Equal(store.GetLatest(scenario.Id)!.FinishedAt, latest.FinishedAt);
        Assert.Equal("https://example.com", latest.Scenario.Target);
        Assert.Equal(0.03, latest.Summary.ErrorRate, 3);
        Assert.Equal(42, latest.Summary.LatencyP95Ms);
        Assert.Equal(97, Assert.Single(latest.StatusCodes).Count);
    }

    [Fact]
    public void DoesNotPersistTimeline()
    {
        var store = new JsonReportStore(HistoryPath);
        var report = Report(new LoadScenario { Name = "s" }, DateTime.UtcNow);
        store.Save(report);

        // В памяти таймлайн остаётся — отчёт сразу после прогона рисует графики.
        Assert.Single(store.GetHistory()[0].Timeline);
        Assert.Empty(new JsonReportStore(HistoryPath).GetHistory()[0].Timeline);
    }

    [Fact]
    public void CorruptedFileStartsWithEmptyHistory()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(HistoryPath, "{ not json");

        var store = new JsonReportStore(HistoryPath);

        Assert.Empty(store.GetHistory());
        store.Save(Report(new LoadScenario { Name = "s" }, DateTime.UtcNow));
        Assert.Single(new JsonReportStore(HistoryPath).GetHistory());
    }

    [Fact]
    public void SaveRaisesChanged()
    {
        var store = new JsonReportStore(HistoryPath);
        var changed = 0;
        store.Changed += () => changed++;

        store.Save(Report(new LoadScenario { Name = "s" }, DateTime.UtcNow));

        Assert.Equal(1, changed);
    }
}
