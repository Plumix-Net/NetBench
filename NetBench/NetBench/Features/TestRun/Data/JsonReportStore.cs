using System.Text.Json;
using System.Text.Json.Serialization;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.TestRun.Data;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<TestRunReport>))]
internal sealed partial class ReportHistoryJsonContext : JsonSerializerContext;

/// <summary>
/// История прогонов, переживающая перезапуск: в памяти — <see cref="InMemoryReportStore"/>,
/// на диск после каждого <see cref="Save"/> уходит вся история одним файлом.
/// <see cref="TestRunReport.Timeline"/> не сохраняется: снапшоты по 4/с раздули бы файл
/// до мегабайт, а восстановленному отчёту хватает сводки и кодов ответов.
/// </summary>
public sealed class JsonReportStore : IReportStore
{
    private readonly InMemoryReportStore _memory = new();
    private readonly string _path;

    public JsonReportStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetBench", "history.json");

        // В файле новые первыми — сохраняем с конца, чтобы и порядок, и «последний» совпали.
        var restored = Load(_path);
        for (var i = restored.Count - 1; i >= 0; i--)
            _memory.Save(restored[i]);
    }

    public event Action? Changed
    {
        add => _memory.Changed += value;
        remove => _memory.Changed -= value;
    }

    public void Save(TestRunReport report)
    {
        _memory.Save(report);
        Persist(_memory.GetHistory());
    }

    public TestRunReport? GetLatest(Guid scenarioId) => _memory.GetLatest(scenarioId);

    public IReadOnlyList<TestRunReport> GetAll() => _memory.GetAll();

    public IReadOnlyList<TestRunReport> GetHistory() => _memory.GetHistory();

    // Раз в прогон и ≤ HistoryLimit сводок без таймлайна — десятки КБ, пишем синхронно.
    private void Persist(IReadOnlyList<TestRunReport> history)
    {
        var compact = new List<TestRunReport>(history.Count);
        foreach (var report in history)
        {
            compact.Add(new TestRunReport
            {
                Scenario = report.Scenario,
                StartedAt = report.StartedAt,
                FinishedAt = report.FinishedAt,
                Summary = report.Summary,
                StatusCodes = report.StatusCodes,
            });
        }

        // Не записалось — история остаётся в памяти до перезапуска; прогон из-за этого не теряем.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(compact, ReportHistoryJsonContext.Default.ListTestRunReport));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static List<TestRunReport> Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllText(path), ReportHistoryJsonContext.Default.ListTestRunReport) ?? []
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }
}
