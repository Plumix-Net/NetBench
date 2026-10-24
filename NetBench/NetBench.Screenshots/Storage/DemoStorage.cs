using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Screenshots.Storage;

/// <summary>
/// Данные, которые видит приложение в текущем кадре: <see cref="ShotSession"/> заполняет их
/// перед созданием <c>MobileShell</c>, а демо-хранилища ниже читают отсюда вместо файлов.
/// </summary>
internal static class DemoStorage
{
    public static AppSettings Settings { get; set; } = new();

    public static List<LoadScenario> Scenarios { get; } = [];

    public static InMemoryReportStore Reports { get; private set; } = new();

    public static void Reset(AppSettings settings, IEnumerable<LoadScenario> scenarios, IEnumerable<TestRunReport> history)
    {
        Settings = settings;
        Scenarios.Clear();
        Scenarios.AddRange(scenarios);
        Reports = new InMemoryReportStore();

        // История — новые первыми, а Save кладёт в начало: сохраняем с конца.
        var reports = history.ToList();
        for (var i = reports.Count - 1; i >= 0; i--)
            Reports.Save(reports[i]);
    }
}
