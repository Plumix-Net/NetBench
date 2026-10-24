using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using Plumix.Rendering;

namespace NetBench.Mobile.Tests.Infrastructure;

/// <summary>Шаги, общие для нескольких сценариев: открыть быстрый тест, запустить и остановить прогон.</summary>
public static class MobileFlows
{
    private static Strings En => Strings.Instance.Root;

    public static void OpenQuickTest(this MobileHarness app, string url)
    {
        app.Tap(En.Mobile.QuickTest);
        // Поле URL получает фокус само (autofocus) — текст идёт в него без тапа
        app.Type(url);
    }

    /// <summary>Узлы ряда пресетов в порядке слева направо.</summary>
    public static IReadOnlyList<SemanticsNode> Presets(this MobileHarness app, IEnumerable<string> labels) =>
        [.. labels.Select(app.GetByLabel)];

    public static bool IsSelected(this SemanticsNode node) => node.Flags.HasFlag(SemanticsFlags.IsSelected);

    public static async Task WaitForMonitorAsync(this MobileHarness app) =>
        await app.PumpUntilAsync(() => app.HasLabel(En.Mobile.Stop), "the monitor screen shows STOP");

    /// <summary>«Стоп» на мониторе и ожидание экрана результата (отчёт к этому моменту сохранён).</summary>
    public static async Task StopRunAsync(this MobileHarness app)
    {
        app.Tap(En.Mobile.Stop);
        await app.PumpUntilAsync(() => app.HasLabel(En.Mobile.ShareReport), "the result screen opens");
    }

    public static IReadOnlyList<TestRunReport> ReadHistory(this MobileHarness app) =>
        new JsonReportStore(Path.Combine(app.DataDirectory, "history.json")).GetHistory();
}
