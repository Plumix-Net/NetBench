using NetBench.Desktop.Controls;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.Report.Presentation.Desktop;

/// <summary>Строка распределения задержек.</summary>
public sealed class LatencyRow(string label, double percentage, string display)
{
    public string Label { get; } = label;
    public double Percentage { get; } = percentage;
    public string Display { get; } = display;
}

/// <summary>Строка таблицы кодов ответов, сгруппированная по классу (2xx/3xx/4xx/5xx/000).</summary>
public sealed class StatusRow(string codeLabel, long count, double pct, MetricTone tone)
{
    public string CodeLabel { get; } = codeLabel;
    public string CountText { get; } = count.ToString("N0");
    public string PctText { get; } = pct.ToString("F1");

    public bool IsSuccess { get; } = tone == MetricTone.Success;
    public bool IsInfo { get; } = tone == MetricTone.Rps;
    public bool IsWarn { get; } = tone == MetricTone.Latency;
    public bool IsError { get; } = tone == MetricTone.Error;
}

/// <summary>Общее форматирование отчёта для экранов «Отчёт» и «Сравнение».</summary>
public static class ReportFormatting
{
    public static string Meta(TestRunReport report) =>
        $"{report.StartedAt.ToLocalTime():dd.MM.yyyy HH:mm} · {report.Summary.Elapsed.TotalSeconds:F0} с";

    public static MetricTone ErrorTone(TestRunStats summary) =>
        summary.ErrorRate >= 0.05 ? MetricTone.Error : MetricTone.Success;

    public static string ErrorPct(TestRunStats summary) => (summary.ErrorRate * 100).ToString("F1");

    public static IReadOnlyList<LatencyRow> LatencyRows(TestRunStats s)
    {
        var max = Math.Max(s.LatencyMaxMs, 1);
        return
        [
            Row("Min", s.LatencyMinMs, max),
            Row("p50", s.LatencyP50Ms, max),
            Row("Mean", s.LatencyMeanMs, max),
            Row("p95", s.LatencyP95Ms, max),
            Row("p99", s.LatencyP99Ms, max),
            Row("Max", s.LatencyMaxMs, max),
        ];

        static LatencyRow Row(string label, double ms, double max) =>
            new(label, ms / max * 100, $"{ms:F0} ms");
    }

    public static IReadOnlyList<StatusRow> StatusRows(TestRunReport report)
    {
        var total = Math.Max(report.Summary.TotalRequests, 1);
        return
        [
            .. report.StatusCodes
                .GroupBy(b => b.StatusCode == 0 ? 0 : b.StatusCode / 100)
                .OrderBy(g => g.Key == 0 ? int.MaxValue : g.Key) // сетевые сбои — в конец
                .Select(g =>
                {
                    var count = g.Sum(b => b.Count);
                    return new StatusRow(
                        codeLabel: g.Key == 0 ? "000" : $"{g.Key}xx",
                        count: count,
                        pct: count * 100.0 / total,
                        tone: g.Key switch
                        {
                            2 => MetricTone.Success,
                            3 => MetricTone.Rps,
                            4 => MetricTone.Latency,
                            _ => MetricTone.Error,
                        });
                }),
        ];
    }

    public static ObservableCollection<ChartPoint> LatencyTimeline(TestRunReport report) =>
        new(report.Timeline.Select(s => new ChartPoint(s.Elapsed.TotalSeconds, s.LatencyP95Ms)));
}
