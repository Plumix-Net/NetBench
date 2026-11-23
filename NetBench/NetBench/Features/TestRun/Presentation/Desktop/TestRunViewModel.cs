using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Desktop.Controls;
using NetBench.Features.TestRun.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.Report.Data;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Desktop.Services;
using NetBench.Localization;

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
    [ObservableProperty] private string _p95Text = Strings.Instance.Root.TestRun.NoValue;
    [ObservableProperty] private string _p95Unit = "";
    [ObservableProperty] private string _errorPctText = "0.0";
    [ObservableProperty] private MetricTone _errorTone = MetricTone.Success;
    [ObservableProperty] private string _totalText = "0";
    [ObservableProperty] private string _usersText = "0";

    /// <summary>Самая частая причина запросов без ответа — чтобы 100% ошибок не оставались загадкой.</summary>
    [ObservableProperty] private string? _topErrorText;

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
            // Пользователь мог уйти на другой сценарий (прогон тогда остановлен) — не выдёргиваем его обратно.
            if (ReferenceEquals(_navigation.Current, this))
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
            // «Стоп» или движок сам остановил прогон (битый запрос) — отчёт всё равно придёт
            // через RunCompleted (finally движка)
        }
        catch (ArgumentException)
        {
            // Невалидный Target: прогон не начался. Сюда не попадаем — адрес проверяется до запуска.
            _navigation.NavigateTo(null);
        }
        finally
        {
            IsRunning = false;
            // Движок одноразовый: освобождаем HttpClient и его пул соединений сразу после прогона.
            await DisposeAsync();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        if (!_disposed)
            _cts?.Cancel();
    }

    private void ApplyStats(TestRunStats stats)
    {
        CurrentStats = stats;

        // Во время прогона — живые цифры за последний интервал: средний с начала RPS сглаживает
        // всё в плоскую линию и не показывает, что сервер «лёг» на середине. Средние — в отчёте.
        RpsText = stats.CurrentRequestsPerSecond.ToString("N0");
        P95Text = stats.RespondedRequests > 0
            ? ReportFormatting.Ms(stats.IntervalLatencyP95Ms > 0 ? stats.IntervalLatencyP95Ms : stats.LatencyP95Ms)
            : Strings.Instance.Root.TestRun.NoValue;
        P95Unit = stats.RespondedRequests > 0 ? Strings.Instance.Root.Common.MillisecondsShort : "";
        ErrorPctText = (stats.ErrorRate * 100).ToString("F1");
        ErrorTone = stats.ErrorRate >= 0.05 ? MetricTone.Error : MetricTone.Success;
        TotalText = stats.TotalRequests.ToString("N0");
        UsersText = $"{stats.ActiveUsers:N0} / {Scenario.Load.ConcurrentUsers:N0}";
        TopErrorText = stats.TopError is { } error
            ? Strings.Instance.Root.TestRun.TopError(ErrorText.Label(error.Kind), ErrorText.Hint(error.Kind))
            : null;

        var duration = Scenario.Load.Duration;
        ProgressPercent = duration > TimeSpan.Zero
            ? Math.Min(100, stats.Elapsed / duration * 100)
            : 0;
        var elapsed = stats.Elapsed > duration ? duration : stats.Elapsed;
        TimeLabel = $"{Format(elapsed)} / {Format(duration)}";

        var t = stats.Elapsed.TotalSeconds;
        AddPoint(RpsPoints, t, stats.CurrentRequestsPerSecond);
        if (stats.IntervalLatencyP95Ms > 0)
            AddPoint(P95Points, t, stats.IntervalLatencyP95Ms);
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";

    private static void AddPoint(ObservableCollection<ChartPoint> pts, double x, double y)
    {
        pts.Add(new ChartPoint(x, y));
        if (pts.Count > 240)
            pts.RemoveAt(0);
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_cts is not null)
            await _cts.CancelAsync();
        await _engine.DisposeAsync();
        _cts?.Dispose();
    }
}
