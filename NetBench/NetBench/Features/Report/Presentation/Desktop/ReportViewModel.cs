using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.TestRun.Domain;
using NetBench.Desktop.Services;

namespace NetBench.Features.Report.Presentation.Desktop;

public partial class ReportViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public TestRunReport Report { get; }
    public IReadOnlyList<LatencyBar> LatencyBars { get; }

    public ReportViewModel(TestRunReport report, INavigationService navigation)
    {
        Report = report;
        _navigation = navigation;

        var s = report.Summary;
        var max = Math.Max(s.LatencyMaxMs, 1);
        LatencyBars =
        [
            new LatencyBar("Min",  s.LatencyMinMs,  max),
            new LatencyBar("p50",  s.LatencyP50Ms,  max),
            new LatencyBar("Mean", s.LatencyMeanMs, max),
            new LatencyBar("p95",  s.LatencyP95Ms,  max),
            new LatencyBar("p99",  s.LatencyP99Ms,  max),
            new LatencyBar("Max",  s.LatencyMaxMs,  max),
        ];
    }

    [RelayCommand]
    private void Close() => _navigation.NavigateTo(null);
}

public sealed class LatencyBar(string label, double value, double max)
{
    public string Label { get; } = label;
    public double Value { get; } = value;
    public double Maximum { get; } = max;
    public string ValueText { get; } = $"{value:F0} ms";
}
