using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Desktop.Controls;
using NetBench.Features.Report.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Desktop.Services;
using NetBench.Localization;

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
    public string TotalsText { get; }

    public string RpsText { get; }
    public string P50Text { get; }
    public string P95Text { get; }
    public string P99Text { get; }
    public string LatencyUnit { get; }

    /// <summary>Сервер ответил хотя бы раз — иначе распределение задержек из одних нулей не показываем.</summary>
    public bool HasLatency { get; }
    public string ErrorPctText { get; }
    public MetricTone ErrorTone { get; }

    /// <summary>Ни один запрос не получил ответа — баннер с причиной.</summary>
    public bool AllFailed { get; }
    public string AllFailedText { get; }

    public IReadOnlyList<ErrorRow> ErrorRows { get; }
    public bool HasErrorRows => ErrorRows.Count > 0;

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
        TotalsText = ReportFormatting.Totals(report);
        RpsText = s.RequestsPerSecond.ToString("N0");
        P50Text = ReportFormatting.Latency(s, s.LatencyP50Ms);
        P95Text = ReportFormatting.Latency(s, s.LatencyP95Ms);
        P99Text = ReportFormatting.Latency(s, s.LatencyP99Ms);
        LatencyUnit = ReportFormatting.LatencyUnit(s);
        HasLatency = ReportFormatting.HasLatency(s);
        ErrorPctText = ReportFormatting.ErrorPct(s);
        ErrorTone = ReportFormatting.ErrorTone(s);
        ErrorRows = ReportFormatting.ErrorRows(report);

        // «Все упали» — это когда ни одного ответа; 100% ответов 500 — другая история, её видно в кодах.
        AllFailed = s.TotalRequests > 0 && report.StatusCodes.All(b => b.StatusCode == 0);
        AllFailedText = report.Errors is [var top, ..]
            ? Strings.Instance.Root.Report.AllFailedReason(ErrorText.Label(top.Kind), ErrorText.Hint(top.Kind))
            : Strings.Instance.Root.Report.AllFailed;
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
