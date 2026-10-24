using System.Globalization;
using System.Text;
using NetBench.Features.Report.Domain;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.Report.Data;

/// <summary>
/// Пишет экспорт <see cref="ReportExporter"/> во временный каталог и передаёт файл в
/// <see cref="IShareSheet"/>. Каталог общий для всех экспортов и чистится перед каждым:
/// получатель успевает забрать файл, пока меню открыто, а старые копии не копятся.
/// </summary>
public sealed class ReportSharer : IReportSharer
{
    private readonly IShareSheet _sheet;
    private readonly string _directory;

    /// <param name="directory">
    /// Каталог для файлов; по умолчанию <c>netbench-share</c> во временном каталоге
    /// (на Android — cache приложения, его же раздаёт FileProvider головы).
    /// </param>
    public ReportSharer(IShareSheet sheet, string? directory = null)
    {
        _sheet = sheet;
        _directory = directory ?? Path.Combine(Path.GetTempPath(), "netbench-share");
    }

    public Task ShareTextAsync(string text, string title) => _sheet.ShareTextAsync(text, title);

    public async Task ShareAsync(TestRunReport report, ReportFormat format, string title)
    {
        var (content, extension, mimeType) = format switch
        {
            ReportFormat.Html => (ReportExporter.ToHtml(report), "html", "text/html"),
            ReportFormat.Csv => (ReportExporter.ToCsv(report), "csv", "text/csv"),
            ReportFormat.Json => (ReportExporter.ToJson(report), "json", "application/json"),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        Directory.CreateDirectory(_directory);

        var path = Path.Combine(_directory, $"{FileStem(report)}.{extension}");
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            .ConfigureAwait(false);

        await _sheet.ShareFileAsync(path, mimeType, title).ConfigureAwait(false);
    }

    /// <summary>«netbench-checkout-flow-20261024-1530»: имя сценария латиницей и цифрами, время прогона.</summary>
    public static string FileStem(TestRunReport report)
    {
        var slug = new StringBuilder();
        foreach (var ch in report.Scenario.Name.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
                slug.Append(ch);
            else if (slug.Length > 0 && slug[^1] != '-')
                slug.Append('-');
        }

        var name = slug.ToString().Trim('-');
        if (name.Length > 40)
            name = name[..40].TrimEnd('-');

        var stamp = report.FinishedAt.ToLocalTime().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return name.Length == 0 ? $"netbench-{stamp}" : $"netbench-{name}-{stamp}";
    }
}
