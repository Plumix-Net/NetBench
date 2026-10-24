using NetBench.Features.Report.Data;
using NetBench.Features.Report.Domain;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

public sealed class ReportSharerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "netbench-tests", Guid.NewGuid().ToString("N"));
    private readonly RecordingShareSheet _sheet = new();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private static TestRunReport Report(string name) => new()
    {
        Scenario = new LoadScenario { Name = name, Target = "https://api.shop.io" },
        StartedAt = new DateTime(2026, 7, 5, 14, 32, 0, DateTimeKind.Local),
        FinishedAt = new DateTime(2026, 7, 5, 14, 33, 0, DateTimeKind.Local),
        Summary = new TestRunStats { TotalRequests = 10, Elapsed = TimeSpan.FromSeconds(60) },
    };

    [Theory]
    [InlineData(ReportFormat.Html, ".html", "text/html", "<html")]
    [InlineData(ReportFormat.Csv, ".csv", "text/csv", "metric,value")]
    [InlineData(ReportFormat.Json, ".json", "application/json", "\"totalRequests\"")]
    public async Task WritesExportAndHandsFileToShareSheet(
        ReportFormat format, string extension, string mimeType, string marker)
    {
        var sharer = new ReportSharer(_sheet, _directory);

        await sharer.ShareAsync(Report("Checkout"), format, "Share");

        var (path, mime, title) = Assert.Single(_sheet.Files);
        Assert.Equal(extension, Path.GetExtension(path));
        Assert.Equal(mimeType, mime);
        Assert.Equal("Share", title);
        Assert.StartsWith(_directory, path, StringComparison.Ordinal);
        Assert.Contains(marker, await File.ReadAllTextAsync(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviousExportIsRemoved()
    {
        var sharer = new ReportSharer(_sheet, _directory);

        await sharer.ShareAsync(Report("First"), ReportFormat.Csv, "Share");
        await sharer.ShareAsync(Report("Second"), ReportFormat.Json, "Share");

        Assert.False(File.Exists(_sheet.Files[0].Path));
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task TextGoesStraightToShareSheet()
    {
        await new ReportSharer(_sheet, _directory).ShareTextAsync("summary", "Share");

        Assert.Equal([("summary", "Share")], _sheet.Texts);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData("Checkout, Flow", "netbench-checkout-flow-20260705-1433")]
    [InlineData("Быстрый тест", "netbench-20260705-1433")]
    [InlineData("  API v2 / health  ", "netbench-api-v2-health-20260705-1433")]
    public void FileStemIsAsciiSlugWithTimestamp(string name, string expected) =>
        Assert.Equal(expected, ReportSharer.FileStem(Report(name)));

    private sealed class RecordingShareSheet : IShareSheet
    {
        public List<(string Path, string MimeType, string Title)> Files { get; } = [];

        public List<(string Text, string Title)> Texts { get; } = [];

        public Task ShareTextAsync(string text, string title)
        {
            Texts.Add((text, title));
            return Task.CompletedTask;
        }

        public Task ShareFileAsync(string path, string mimeType, string title)
        {
            Files.Add((path, mimeType, title));
            return Task.CompletedTask;
        }
    }
}
