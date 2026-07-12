using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Desktop.Controls;
using NetBench.Features.TestRun.Domain;
using NetBench.Desktop.Services;

namespace NetBench.Features.Report.Presentation.Desktop;

/// <summary>Вариант в выпадающем списке сравнения — последний отчёт сценария.</summary>
public sealed class CompareOption(TestRunReport report)
{
    public TestRunReport Report { get; } = report;
    public string Label { get; } =
        $"{report.Scenario.Name} · {report.StartedAt.ToLocalTime():dd.MM HH:mm}";
}

/// <summary>Одна колонка сравнения — сводка отчёта.</summary>
public sealed class CompareSide(TestRunReport report)
{
    public string Name { get; } = report.Scenario.Name;
    public string Target { get; } = report.Scenario.Target;
    public string MetaText { get; } = ReportFormatting.Meta(report);
    public string RpsText { get; } = report.Summary.RequestsPerSecond.ToString("N0");
    public string P95Text { get; } = report.Summary.LatencyP95Ms.ToString("F0");
    public string ErrorPctText { get; } = ReportFormatting.ErrorPct(report.Summary);
    public MetricTone ErrorTone { get; } = ReportFormatting.ErrorTone(report.Summary);
    public IReadOnlyList<LatencyRow> LatencyRows { get; } = ReportFormatting.LatencyRows(report.Summary);
}

public partial class CompareViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public IReadOnlyList<CompareOption> Options { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SideA))]
    private CompareOption? _selectedA;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SideB))]
    private CompareOption? _selectedB;

    /// <summary>Экран отчёта, с которого открыли сравнение, — для «Назад».</summary>
    public ObservableObject? BackPage { get; set; }

    public CompareViewModel(TestRunReport current, IReportStore reports, INavigationService navigation)
    {
        _navigation = navigation;

        Options = [.. reports.GetAll().Select(r => new CompareOption(r))];
        _selectedA = Options.FirstOrDefault(o => o.Report == current) ?? Options.FirstOrDefault();
        _selectedB = Options.FirstOrDefault(o => o != _selectedA) ?? _selectedA;
    }

    public CompareSide? SideA => SelectedA is { } a ? new CompareSide(a.Report) : null;
    public CompareSide? SideB => SelectedB is { } b ? new CompareSide(b.Report) : null;

    [RelayCommand]
    private void Back() => _navigation.NavigateTo(BackPage);
}
