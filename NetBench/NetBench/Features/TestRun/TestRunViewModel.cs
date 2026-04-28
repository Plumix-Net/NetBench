using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Core.Engine;
using NetBench.Core.Models;
using NetBench.Features.Report;
using NetBench.Services;

namespace NetBench.Features.TestRun;

public partial class TestRunViewModel : ObservableObject, IAsyncDisposable
{
    private readonly LoadEngine _engine;
    private readonly INavigationService _navigation;
    private readonly Func<TestRunReport, ReportViewModel> _createReport;
    private CancellationTokenSource? _cts;

    public LoadScenario Scenario { get; }

    [ObservableProperty] private TestRunStats? _currentStats;
    [ObservableProperty] private bool _isRunning;

    public TestRunViewModel(
        LoadScenario scenario,
        INavigationService navigation,
        Func<TestRunReport, ReportViewModel> createReport)
    {
        Scenario = scenario;
        _navigation = navigation;
        _createReport = createReport;

        _engine = new LoadEngine();
        _engine.StatsUpdated += stats => CurrentStats = stats;
        _engine.RunCompleted += report => _navigation.NavigateTo(_createReport(report));
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
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        await _engine.DisposeAsync();
        _cts?.Dispose();
    }
}
