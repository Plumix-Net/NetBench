using NetBench.Features.Scenarios.Domain;

namespace NetBench.Localization;

public static class ScenarioDisplayName
{
    /// <summary>
    /// Имя сценария для UI: быстрый тест — на текущем языке (а не на том, что был при запуске),
    /// у безымянного — локализованная заглушка, а не пустая строка.
    /// </summary>
    public static string DisplayNameOf(this Strings strings, LoadScenario scenario) =>
        scenario.IsQuickTest
            ? strings.Mobile.QuickTest
            : string.IsNullOrWhiteSpace(scenario.Name)
                ? strings.Scenarios.Unnamed
                : scenario.Name;

    /// <summary>«15 с», «1 мин», «5 мин» — подпись пресета длительности.</summary>
    public static string DurationLabel(this Strings strings, TimeSpan duration) =>
        duration < TimeSpan.FromMinutes(1)
            ? strings.Mobile.DurationSeconds(((int)duration.TotalSeconds).ToString(DisplayCulture.Current))
            : strings.Mobile.DurationMinutes(duration.TotalMinutes.ToString("0.#", DisplayCulture.Current));
}
