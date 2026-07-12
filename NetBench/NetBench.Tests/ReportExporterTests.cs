using NetBench.Features.Report.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using System.Text.Json;
using Xunit;

namespace NetBench.Tests;

public class ReportExporterTests
{
    private static TestRunReport SampleReport() => new()
    {
        Scenario = new LoadScenario { Name = "Checkout, Flow", Target = "https://api.shop.io/checkout" },
        StartedAt = new DateTime(2026, 7, 5, 14, 32, 0, DateTimeKind.Utc),
        FinishedAt = new DateTime(2026, 7, 5, 14, 33, 0, DateTimeKind.Utc),
        Summary = new TestRunStats
        {
            TotalRequests = 31_200,
            ErrorCount = 125,
            RequestsPerSecond = 520.4,
            Elapsed = TimeSpan.FromSeconds(60),
            LatencyMinMs = 29,
            LatencyP50Ms = 72,
            LatencyMeanMs = 84,
            LatencyP95Ms = 145,
            LatencyP99Ms = 196,
            LatencyMaxMs = 319,
            TotalBytesReceived = 1_000_000,
        },
        StatusCodes =
        [
            new StatusCodeBucket { StatusCode = 0, Count = 25 },
            new StatusCodeBucket { StatusCode = 200, Count = 31_075 },
            new StatusCodeBucket { StatusCode = 500, Count = 100 },
        ],
    };

    [Fact]
    public void JsonContainsSummaryAndStatusCodes()
    {
        var json = ReportExporter.ToJson(SampleReport());

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Checkout, Flow", root.GetProperty("scenarioName").GetString());
        Assert.Equal(31_200, root.GetProperty("totalRequests").GetInt64());
        Assert.Equal(145, root.GetProperty("latencyMs").GetProperty("p95").GetDouble());
        Assert.Equal(3, root.GetProperty("statusCodes").GetArrayLength());
    }

    [Fact]
    public void CsvEscapesCommasAndListsStatusCodes()
    {
        var csv = ReportExporter.ToCsv(SampleReport());

        Assert.Contains("scenario,\"Checkout, Flow\"", csv);
        Assert.Contains("rps,520.4", csv);
        Assert.Contains("200,31075", csv);
        Assert.Contains("0,25", csv);
    }

    [Fact]
    public void HtmlIsSelfContainedAndEscaped()
    {
        var report = SampleReport();
        report.Scenario.Name = "A<B & C";

        var html = ReportExporter.ToHtml(report);

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("A&lt;B &amp; C", html);
        Assert.Contains("000", html); // сетевые сбои показываются как «000»
        Assert.DoesNotContain("A<B", html);
    }
}
