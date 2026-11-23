using NetBench.Desktop.Controls;
using NetBench.Features.Report.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;

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

/// <summary>Строка таблицы «Запросы без ответа»: причина, что с ней делать и точный текст исключения.</summary>
public sealed class ErrorRow(ErrorBucket bucket, long total)
{
    public string Label { get; } = ErrorText.Label(bucket.Kind);
    public string Hint { get; } = ErrorText.Hint(bucket.Kind);
    public string? Message { get; } = bucket.Message;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public string CountText { get; } = bucket.Count.ToString("N0");
    public string PctText { get; } = (bucket.Count * 100.0 / Math.Max(total, 1)).ToString("F1");
}

/// <summary>Общее форматирование отчёта для экранов «Отчёт» и «Сравнение».</summary>
public static class ReportFormatting
{
    public static string Meta(TestRunReport report) => Strings.Instance.Root.Report.Meta(
        report.StartedAt.ToLocalTime().ToString("g"),
        report.Summary.Elapsed.TotalSeconds.ToString("F0"));

    /// <summary>Запросов, пользователей, получено данных — масштаб прогона под заголовком отчёта.</summary>
    public static string Totals(TestRunReport report) => Strings.Instance.Root.Report.Totals(
        report.Summary.TotalRequests.ToString("N0"),
        report.Scenario.Load.ConcurrentUsers.ToString("N0"),
        ErrorText.Bytes(report.Summary.TotalBytesReceived));

    /// <summary>
    /// Задержка в мс или «—», если сервер не ответил ни разу: нулевая задержка была бы враньём.
    /// У отчётов из истории до появления <see cref="TestRunStats.RespondedRequests"/> поле равно 0,
    /// но задержки посчитаны — их узнаём по ненулевому максимуму.
    /// </summary>
    public static string Latency(TestRunStats summary, double ms) =>
        summary.RespondedRequests > 0 || summary.LatencyMaxMs > 0 || summary.TotalRequests == 0
            ? Ms(ms)
            : Strings.Instance.Root.TestRun.NoValue;

    /// <summary>Миллисекунды: до 10 мс — с десятыми (на быстрых API «0 ms» ничего не говорит), дальше — целыми.</summary>
    public static string Ms(double ms) => ms is > 0 and < 10 ? ms.ToString("F1") : ms.ToString("F0");

    /// <summary>Есть ли что показывать в задержках (см. <see cref="Latency"/>).</summary>
    public static bool HasLatency(TestRunStats summary) =>
        summary.RespondedRequests > 0 || summary.LatencyMaxMs > 0;

    /// <summary>Единица к задержке: у прочерка её нет.</summary>
    public static string LatencyUnit(TestRunStats summary) =>
        HasLatency(summary) || summary.TotalRequests == 0 ? Strings.Instance.Root.Common.MillisecondsShort : "";

    public static IReadOnlyList<ErrorRow> ErrorRows(TestRunReport report) =>
        [.. report.Errors.Select(e => new ErrorRow(e, report.Summary.TotalRequests))];

    public static MetricTone ErrorTone(TestRunStats summary) =>
        summary.ErrorRate >= 0.05 ? MetricTone.Error : MetricTone.Success;

    public static string ErrorPct(TestRunStats summary) => (summary.ErrorRate * 100).ToString("F1");

    public static IReadOnlyList<LatencyRow> LatencyRows(TestRunStats s)
    {
        var max = Math.Max(s.LatencyMaxMs, 1);
        return
        [
            Row(Strings.Instance.Root.Report.LatencyMin, s.LatencyMinMs, max),
            Row(Strings.Instance.Root.Common.P50, s.LatencyP50Ms, max),
            Row(Strings.Instance.Root.Report.LatencyMean, s.LatencyMeanMs, max),
            Row(Strings.Instance.Root.Common.P95, s.LatencyP95Ms, max),
            Row(Strings.Instance.Root.Common.P99, s.LatencyP99Ms, max),
            Row(Strings.Instance.Root.Report.LatencyMax, s.LatencyMaxMs, max),
        ];

        static LatencyRow Row(string label, double ms, double max) =>
            new(label, ms / max * 100,
                Strings.Instance.Root.Report.LatencyValue(Ms(ms)));
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
