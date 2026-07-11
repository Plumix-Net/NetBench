using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Desktop.Controls;
using NetBench.Features.TestRun.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Desktop.Services;

namespace NetBench.Features.TestRun.Presentation.Desktop;

public partial class TestRunViewModel : ObservableObject, IAsyncDisposable
{
    private readonly LoadEngine _engine;
    private readonly INavigationService _navigation;
    private readonly Func<TestRunReport, ReportViewModel> _createReport;
    private CancellationTokenSource? _cts;

    public LoadScenario Scenario { get; }

    [ObservableProperty] private TestRunStats? _currentStats;
    [ObservableProperty] private bool _isRunning;

    public ObservableCollection<ChartPoint> RpsPoints { get; } = [];
    public ObservableCollection<ChartPoint> P95Points { get; } = [];

    public TestRunViewModel(
        LoadScenario scenario,
        INavigationService navigation,
        Func<TestRunReport, ReportViewModel> createReport)
    {
        Scenario = scenario;
        _navigation = navigation;
        _createReport = createReport;

        _engine = new LoadEngine();
        _engine.StatsUpdated += stats => Dispatcher.UIThread.Post(() =>
        {
            CurrentStats = stats;
            var t = stats.Elapsed.TotalSeconds;
            AddPoint(RpsPoints, t, stats.RequestsPerSecond);
            AddPoint(P95Points, t, stats.LatencyP95Ms);
        });
        _engine.RunCompleted += report => Dispatcher.UIThread.Post(() =>
            _navigation.NavigateTo(_createReport(report)));
    }

    [RelayCommand]
    private async Task StartAsync()
    {
        IsRunning = true;
        _cts = new CancellationTokenSource();
        try
        {
            await _engine.RunAsync(Scenario, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // user pressed Stop — report is still fired via RunCompleted in LoadEngine.finally
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    private static void AddPoint(ObservableCollection<ChartPoint> pts, double x, double y)
    {
        pts.Add(new ChartPoint(x, y));
        if (pts.Count > 120)
            pts.RemoveAt(0);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
            await _cts.CancelAsync();
        await _engine.DisposeAsync();
        _cts?.Dispose();
    }
}
