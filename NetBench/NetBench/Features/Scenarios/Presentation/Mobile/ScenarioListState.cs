using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

public enum ScenarioListStatus
{
    Loading,
    Ready,
    Failure,
}

/// <summary>Иммутабельное состояние мобильного экрана списка сценариев.</summary>
public sealed record ScenarioListState(
    ScenarioListStatus Status,
    IReadOnlyList<LoadScenario> Scenarios,
    string? Error = null,
    IReadOnlyDictionary<Guid, TestRunReport>? LastRuns = null)
{
    public static ScenarioListState Initial { get; } = new(ScenarioListStatus.Loading, []);

    /// <summary>Последний отчёт сценария — для чипа статуса на карточке.</summary>
    public TestRunReport? LastRunOf(Guid scenarioId) => LastRuns?.GetValueOrDefault(scenarioId);
}
