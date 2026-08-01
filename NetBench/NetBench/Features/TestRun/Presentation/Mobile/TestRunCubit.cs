using Avalonia.Threading;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using Plumix.Bloc;

namespace NetBench.Features.TestRun.Presentation.Mobile;

/// <summary>
/// Стейт-менеджер прогона нагрузки — мобильный аналог <c>TestRunViewModel</c>:
/// тот же <see cref="LoadEngine"/>, но снапшоты статистики раздаются через Cubit.
/// </summary>
public sealed class TestRunCubit : Cubit<TestRunState>
{
    private const int MaxPoints = 240;

    private readonly LoadEngine _engine = new();
    private readonly IReportStore _reports;
    private CancellationTokenSource? _cts;

    public LoadScenario Scenario { get; }

    public TestRunCubit(LoadScenario scenario, IReportStore reports)
        : base(TestRunState.Initial)
    {
        Scenario = scenario;
        _reports = reports;
        _engine.StatsUpdated += stats => Dispatcher.UIThread.Post(() => ApplyStats(stats));
        _engine.RunCompleted += report => Dispatcher.UIThread.Post(() => CompleteRun(report));
    }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        try
        {
            await _engine.RunAsync(Scenario, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // пользователь нажал «Стоп» — отчёт всё равно придёт через RunCompleted (finally движка)
        }
    }

    public void Stop() => _cts?.Cancel();

    private void ApplyStats(TestRunStats stats)
    {
        if (IsClosed || State.Phase == TestRunPhase.Completed)
            return;

        var points = new List<double>(Math.Min(State.RpsPoints.Count + 1, MaxPoints));
        var skip = State.RpsPoints.Count + 1 - MaxPoints;
        for (var i = Math.Max(0, skip); i < State.RpsPoints.Count; i++)
            points.Add(State.RpsPoints[i]);
        points.Add(stats.RequestsPerSecond);

        Emit(State with { Stats = stats, RpsPoints = points });
    }

    private void CompleteRun(TestRunReport report)
    {
        if (IsClosed)
            return;

        _reports.Save(report);
        Emit(State with { Phase = TestRunPhase.Completed, Stats = report.Summary, Report = report });
    }

    public override void Close()
    {
        _cts?.Cancel();
        base.Close();
        _ = CleanupAsync();
    }

    private async Task CleanupAsync()
    {
        await _engine.DisposeAsync();
        _cts?.Dispose();
    }
}
