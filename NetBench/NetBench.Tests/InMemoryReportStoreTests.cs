using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

public class InMemoryReportStoreTests
{
    private static TestRunReport Report(LoadScenario scenario, DateTime finishedAt) => new()
    {
        Scenario = scenario,
        StartedAt = finishedAt.AddSeconds(-30),
        FinishedAt = finishedAt,
        Summary = new TestRunStats(),
    };

    [Fact]
    public void KeepsOnlyLatestReportPerScenario()
    {
        var store = new InMemoryReportStore();
        var scenario = new LoadScenario { Name = "s" };
        var older = Report(scenario, DateTime.UtcNow.AddMinutes(-5));
        var newer = Report(scenario, DateTime.UtcNow);

        store.Save(older);
        store.Save(newer);

        Assert.Same(newer, store.GetLatest(scenario.Id));
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void GetAllReturnsNewestFirstAndRaisesChanged()
    {
        var store = new InMemoryReportStore();
        var changed = 0;
        store.Changed += () => changed++;

        var a = Report(new LoadScenario { Name = "a" }, DateTime.UtcNow.AddMinutes(-1));
        var b = Report(new LoadScenario { Name = "b" }, DateTime.UtcNow);
        store.Save(a);
        store.Save(b);

        var all = store.GetAll();
        Assert.Equal(2, changed);
        Assert.Equal(["b", "a"], all.Select(r => r.Scenario.Name).ToArray());
        Assert.Null(store.GetLatest(Guid.NewGuid()));
    }

    [Fact]
    public void GetHistoryKeepsEveryRunNewestFirst()
    {
        var store = new InMemoryReportStore();
        var scenario = new LoadScenario { Name = "s" };
        // Разовый прогон «быстрого теста»: своего сценария в репозитории нет,
        // и без истории такой отчёт нигде не показать.
        var quick = new LoadScenario { Name = "quick" };

        store.Save(Report(scenario, DateTime.UtcNow.AddMinutes(-5)));
        store.Save(Report(quick, DateTime.UtcNow.AddMinutes(-3)));
        store.Save(Report(scenario, DateTime.UtcNow));

        Assert.Equal(["s", "quick", "s"], store.GetHistory().Select(r => r.Scenario.Name).ToArray());
        Assert.Equal(2, store.GetAll().Count);
    }

    [Fact]
    public void GetHistoryDropsRunsBeyondTheLimit()
    {
        var store = new InMemoryReportStore();
        var started = DateTime.UtcNow.AddHours(-2);

        for (var i = 0; i <= InMemoryReportStore.HistoryLimit; i++)
            store.Save(Report(new LoadScenario { Name = i.ToString() }, started.AddMinutes(i)));

        var history = store.GetHistory();
        Assert.Equal(InMemoryReportStore.HistoryLimit, history.Count);
        Assert.Equal(InMemoryReportStore.HistoryLimit.ToString(), history[0].Scenario.Name);
        Assert.Equal("1", history[^1].Scenario.Name);
    }
}
