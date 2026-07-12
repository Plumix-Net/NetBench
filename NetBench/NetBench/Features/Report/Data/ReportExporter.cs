using NetBench.Features.TestRun.Domain;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetBench.Localization;

namespace NetBench.Features.Report.Data;

/// <summary>Экспорт отчёта прогона в JSON/CSV/HTML. Чистые функции — без файловой системы.</summary>
public static class ReportExporter
{
    public static string ToJson(TestRunReport report)
    {
        var s = report.Summary;
        var dto = new ReportExportDto(
            ScenarioName: report.Scenario.Name,
            Target: report.Scenario.Target,
            StartedAt: report.StartedAt,
            FinishedAt: report.FinishedAt,
            DurationSeconds: Math.Round(s.Elapsed.TotalSeconds, 1),
            RequestsPerSecond: Math.Round(s.RequestsPerSecond, 1),
            TotalRequests: s.TotalRequests,
            ErrorCount: s.ErrorCount,
            ErrorRatePercent: Math.Round(s.ErrorRate * 100, 2),
            TotalBytesReceived: s.TotalBytesReceived,
            LatencyMs: new LatencyDto(
                Min: Round(s.LatencyMinMs),
                P50: Round(s.LatencyP50Ms),
                Mean: Round(s.LatencyMeanMs),
                P95: Round(s.LatencyP95Ms),
                P99: Round(s.LatencyP99Ms),
                Max: Round(s.LatencyMaxMs)),
            StatusCodes: [.. report.StatusCodes.Select(b => new StatusCodeDto(b.StatusCode, b.Count))]);

        return JsonSerializer.Serialize(dto, ReportExportJsonContext.Default.ReportExportDto);
    }

    public static string ToCsv(TestRunReport report)
    {
        var s = report.Summary;
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine("metric,value");
        sb.AppendLine($"scenario,{Escape(report.Scenario.Name)}");
        sb.AppendLine($"target,{Escape(report.Scenario.Target)}");
        sb.AppendLine($"started_at,{report.StartedAt.ToString("O", inv)}");
        sb.AppendLine($"finished_at,{report.FinishedAt.ToString("O", inv)}");
        sb.AppendLine($"duration_s,{s.Elapsed.TotalSeconds.ToString("F1", inv)}");
        sb.AppendLine($"rps,{s.RequestsPerSecond.ToString("F1", inv)}");
        sb.AppendLine($"total_requests,{s.TotalRequests.ToString(inv)}");
        sb.AppendLine($"error_count,{s.ErrorCount.ToString(inv)}");
        sb.AppendLine($"error_rate_pct,{(s.ErrorRate * 100).ToString("F2", inv)}");
        sb.AppendLine($"bytes_received,{s.TotalBytesReceived.ToString(inv)}");
        sb.AppendLine($"latency_min_ms,{s.LatencyMinMs.ToString("F1", inv)}");
        sb.AppendLine($"latency_p50_ms,{s.LatencyP50Ms.ToString("F1", inv)}");
        sb.AppendLine($"latency_mean_ms,{s.LatencyMeanMs.ToString("F1", inv)}");
        sb.AppendLine($"latency_p95_ms,{s.LatencyP95Ms.ToString("F1", inv)}");
        sb.AppendLine($"latency_p99_ms,{s.LatencyP99Ms.ToString("F1", inv)}");
        sb.AppendLine($"latency_max_ms,{s.LatencyMaxMs.ToString("F1", inv)}");

        sb.AppendLine();
        sb.AppendLine("status_code,count");
        foreach (var bucket in report.StatusCodes)
            sb.AppendLine($"{bucket.StatusCode.ToString(inv)},{bucket.Count.ToString(inv)}");

        return sb.ToString();
    }

    public static string ToHtml(TestRunReport report)
    {
        var s = report.Summary;
        var inv = CultureInfo.InvariantCulture;
        var strings = Strings.Instance.Root;
        var culture = Strings.SupportedCultures.FirstOrDefault(candidate =>
                candidate.Equals(CultureInfo.CurrentUICulture))
            ?? Strings.SupportedCultures.FirstOrDefault(candidate =>
                candidate.TwoLetterISOLanguageName == CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            ?? Strings.BaseCulture;

        var statusRows = new StringBuilder();
        foreach (var bucket in report.StatusCodes)
        {
            var pct = s.TotalRequests > 0 ? bucket.Count * 100.0 / s.TotalRequests : 0;
            statusRows.Append(inv,
                $"<tr><td>{(bucket.StatusCode == 0 ? "000" : bucket.StatusCode.ToString(inv))}</td>" +
                $"<td>{bucket.Count.ToString("N0", inv)}</td><td>{pct.ToString("F1", inv)}" +
                $"{Html(strings.Common.Percent)}</td></tr>");
        }

        // Палитра — токены дизайн-системы NetBench (тёмная тема).
        return $$"""
            <!DOCTYPE html>
            <html lang="{{culture.Name}}">
            <head>
            <meta charset="utf-8">
            <title>NetBench · {{Html(report.Scenario.Name)}}</title>
            <style>
            body{background:#101014;color:#EDEDF0;font-family:Inter,system-ui,sans-serif;margin:0;padding:40px}
            .mono{font-family:'JetBrains Mono',SF Mono,Menlo,Consolas,monospace;font-variant-numeric:tabular-nums}
            h1{font-size:22px;font-weight:800;color:#fff;margin:0 0 4px}
            .target{color:#8A8A94;font-size:13px}
            .meta{color:#6A6A74;font-size:12px;margin-top:8px}
            .cards{display:grid;grid-template-columns:repeat(5,1fr);gap:14px;margin:26px 0}
            .card{background:#1E1E24;border-radius:8px;padding:16px 18px}
            .card .l{font-size:11px;color:#8A8A94;text-transform:uppercase;letter-spacing:.05em;font-weight:700}
            .card .v{font-size:30px;font-weight:800;margin-top:8px}
            .rps{color:#1E90FF}.lat{color:#FFA500}.err{color:#FF4D4F}.ok{color:#52C41A}
            table{border-collapse:collapse;background:#1E1E24;border-radius:8px;overflow:hidden;min-width:420px}
            th,td{text-align:left;padding:10px 18px;border-bottom:1px solid #232329;font-size:13px}
            th{background:#16161A;color:#6A6A74;text-transform:uppercase;font-size:11px;letter-spacing:.05em}
            h2{font-size:12px;font-weight:700;color:#8A8A94;text-transform:uppercase;letter-spacing:.05em;margin:26px 0 14px}
            </style>
            </head>
            <body>
            <h1>{{Html(report.Scenario.Name)}}</h1>
            <div class="target mono">{{Html(report.Scenario.Target)}}</div>
            <div class="meta mono">{{Html(strings.Report.Meta(report.StartedAt.ToLocalTime().ToString("g"), s.Elapsed.TotalSeconds.ToString("F0")))}}</div>
            <div class="cards">
            <div class="card"><div class="l">{{Html(strings.Common.Rps)}}</div><div class="v mono rps">{{s.RequestsPerSecond.ToString("N0", inv)}}</div></div>
            <div class="card"><div class="l">{{Html(strings.Common.P50)}}</div><div class="v mono lat">{{s.LatencyP50Ms.ToString("F0", inv)}}<span style="font-size:15px">{{Html(strings.Common.MillisecondsShort)}}</span></div></div>
            <div class="card"><div class="l">{{Html(strings.Common.P95)}}</div><div class="v mono lat">{{s.LatencyP95Ms.ToString("F0", inv)}}<span style="font-size:15px">{{Html(strings.Common.MillisecondsShort)}}</span></div></div>
            <div class="card"><div class="l">{{Html(strings.Common.P99)}}</div><div class="v mono lat">{{s.LatencyP99Ms.ToString("F0", inv)}}<span style="font-size:15px">{{Html(strings.Common.MillisecondsShort)}}</span></div></div>
            <div class="card"><div class="l">{{Html(strings.Report.Html.Errors)}}</div><div class="v mono {{(s.ErrorRate >= 0.05 ? "err" : "ok")}}">{{(s.ErrorRate * 100).ToString("F1", inv)}}<span style="font-size:15px">{{Html(strings.Common.Percent)}}</span></div></div>
            </div>
            <h2>{{Html(strings.Report.Html.ResponseCodes)}}</h2>
            <table><tr><th>{{Html(strings.Report.Html.Code)}}</th><th>{{Html(strings.Report.Html.Count)}}</th><th>{{Html(strings.Common.Percent)}}</th></tr>{{statusRows}}</table>
            </body>
            </html>
            """;
    }

    private static double Round(double ms) => Math.Round(ms, 1);

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    private static string Html(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}

public sealed record ReportExportDto(
    string ScenarioName,
    string Target,
    DateTime StartedAt,
    DateTime FinishedAt,
    double DurationSeconds,
    double RequestsPerSecond,
    long TotalRequests,
    long ErrorCount,
    double ErrorRatePercent,
    long TotalBytesReceived,
    LatencyDto LatencyMs,
    IReadOnlyList<StatusCodeDto> StatusCodes);

public sealed record LatencyDto(double Min, double P50, double Mean, double P95, double P99, double Max);

public sealed record StatusCodeDto(int Code, long Count);

// Source-generated сериализация — рефлексия ломает trimming на мобильных таргетах.
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ReportExportDto))]
internal sealed partial class ReportExportJsonContext : JsonSerializerContext;
