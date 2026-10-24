using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.Report.Domain;

public enum ReportFormat
{
    Html,
    Csv,
    Json,
}

/// <summary>Отдаёт отчёт прогона в системное меню «Поделиться» — текстом или файлом экспорта.</summary>
public interface IReportSharer
{
    Task ShareTextAsync(string text, string title);

    Task ShareAsync(TestRunReport report, ReportFormat format, string title);
}
