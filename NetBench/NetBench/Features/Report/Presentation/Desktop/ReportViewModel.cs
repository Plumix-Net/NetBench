using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Desktop.Controls;
using NetBench.Features.Report.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Desktop.Services;

namespace NetBench.Features.Report.Presentation.Desktop;

public partial class ReportViewModel : ObservableObject
{
    private readonly INavigationService _navigation;
    private readonly IFileSaveService _files;
    private readonly Func<TestRunReport, CompareViewModel> _createCompare;

    public TestRunReport Report { get; }

    public string ScenarioName => Report.Scenario.Name;
    public string TargetText => Report.Scenario.Target;
    public string MetaText { get; }

    public string RpsText { get; }
    public string P50Text { get; }
    public string P95Text { get; }
    public string P99Text { get; }
    public string ErrorPctText { get; }
    public MetricTone ErrorTone { get; }

    /// <summary>Все запросы упали — баннер «хост не принял ни одного соединения».</summary>
    public bool AllFailed { get; }

    public IReadOnlyList<LatencyRow> LatencyRows { get; }
    public IReadOnlyList<StatusRow> StatusRows { get; }
    public ObservableCollection<ChartPoint> LatencyTimeline { get; }
    public bool HasTimeline => LatencyTimeline.Count >= 2;

    public ReportViewModel(
        TestRunReport report,
        INavigationService navigation,
        IFileSaveService files,
        Func<TestRunReport, CompareViewModel> createCompare)
    {
        Report = report;
        _navigation = navigation;
        _files = files;
        _createCompare = createCompare;

        var s = report.Summary;
        MetaText = ReportFormatting.Meta(report);
        RpsText = s.RequestsPerSecond.ToString("N0");
        P50Text = s.LatencyP50Ms.ToString("F0");
        P95Text = s.LatencyP95Ms.ToString("F0");
        P99Text = s.LatencyP99Ms.ToString("F0");
        ErrorPctText = ReportFormatting.ErrorPct(s);
        ErrorTone = ReportFormatting.ErrorTone(s);
        AllFailed = s.TotalRequests > 0 && s.ErrorCount == s.TotalRequests;
        LatencyRows = ReportFormatting.LatencyRows(s);
        StatusRows = ReportFormatting.StatusRows(report);
        LatencyTimeline = ReportFormatting.LatencyTimeline(report);
    }

    [RelayCommand]
    private Task ExportJsonAsync(CancellationToken ct) =>
        _files.SaveTextAsync(FileName("json"), ReportExporter.ToJson(Report), ct);

    [RelayCommand]
    private Task ExportHtmlAsync(CancellationToken ct) =>
        _files.SaveTextAsync(FileName("html"), ReportExporter.ToHtml(Report), ct);

    [RelayCommand]
    private Task ExportCsvAsync(CancellationToken ct) =>
        _files.SaveTextAsync(FileName("csv"), ReportExporter.ToCsv(Report), ct);

    [RelayCommand]
    private void Compare()
    {
        var compare = _createCompare(Report);
        compare.BackPage = this;
        _navigation.NavigateTo(compare);
    }

    private string FileName(string ext)
    {
        var name = string.Join("-", ScenarioName.Split(Path.GetInvalidFileNameChars(),
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return $"netbench-{name}-{Report.StartedAt.ToLocalTime():yyyyMMdd-HHmm}.{ext}";
    }
}
