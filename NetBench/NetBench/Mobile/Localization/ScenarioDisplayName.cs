using NetBench.Features.Scenarios.Domain;

namespace NetBench.Localization;

public static class ScenarioDisplayName
{
    /// <summary>Имя сценария для UI: у безымянного — локализованная заглушка, а не пустая строка.</summary>
    public static string DisplayNameOf(this Strings strings, LoadScenario scenario) =>
        string.IsNullOrWhiteSpace(scenario.Name)
            ? strings.Scenarios.Unnamed
            : scenario.Name;
}
