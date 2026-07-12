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
    private readonly IReportStore _reports;
    private readonly Func<TestRunReport, ReportViewModel> _createReport;
    private CancellationTokenSource? _cts;

    public LoadScenario Scenario { get; }

    [ObservableProperty] private TestRunStats? _currentStats;
    [ObservableProperty] private bool _isRunning;

    // Отформатированные значения для карточек метрик
    [ObservableProperty] private string _rpsText = "0";
    [ObservableProperty] private string _p95Text = "0";
    [ObservableProperty] private string _errorPctText = "0.0";
    [ObservableProperty] private MetricTone _errorTone = MetricTone.Success;
    [ObservableProperty] private string _totalText = "0";
    [ObservableProperty] private string _connectionsText = "0";

    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _timeLabel;

    public ObservableCollection<ChartPoint> RpsPoints { get; } = [];
    public ObservableCollection<ChartPoint> P95Points { get; } = [];

    public string TargetLabel => Scenario.Target;

    public TestRunViewModel(
        LoadScenario scenario,
        INavigationService navigation,
        IReportStore reports,
        Func<TestRunReport, ReportViewModel> createReport)
    {
        Scenario = scenario;
        _navigation = navigation;
        _reports = reports;
        _createReport = createReport;
        _timeLabel = $"00:00 / {Format(scenario.Load.Duration)}";

        _engine = new LoadEngine();
        _engine.StatsUpdated += stats => Dispatcher.UIThread.Post(() => ApplyStats(stats));
        _engine.RunCompleted += report => Dispatcher.UIThread.Post(() =>
        {
            _reports.Save(report);
            _navigation.NavigateTo(_createReport(report));
        });

        // Экран прогона открывается уже «записывающим» — запуск сразу
        StartCommand.Execute(null);
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
            // пользователь нажал «Стоп» — отчёт всё равно придёт через RunCompleted (finally движка)
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    private void ApplyStats(TestRunStats stats)
    {
        CurrentStats = stats;

        RpsText = stats.RequestsPerSecond.ToString("N0");
        P95Text = stats.LatencyP95Ms.ToString("F0");
        ErrorPctText = (stats.ErrorRate * 100).ToString("F1");
        ErrorTone = stats.ErrorRate >= 0.05 ? MetricTone.Error : MetricTone.Success;
        TotalText = stats.TotalRequests.ToString("N0");
        ConnectionsText = stats.ActiveConnections.ToString("N0");

        var duration = Scenario.Load.Duration;
        ProgressPercent = duration > TimeSpan.Zero
            ? Math.Min(100, stats.Elapsed / duration * 100)
            : 0;
        var elapsed = stats.Elapsed > duration ? duration : stats.Elapsed;
        TimeLabel = $"{Format(elapsed)} / {Format(duration)}";

        var t = stats.Elapsed.TotalSeconds;
        AddPoint(RpsPoints, t, stats.RequestsPerSecond);
        AddPoint(P95Points, t, stats.LatencyP95Ms);
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";

    private static void AddPoint(ObservableCollection<ChartPoint> pts, double x, double y)
    {
        pts.Add(new ChartPoint(x, y));
        if (pts.Count > 240)
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
