using NetBench.Features.Scenarios.Domain;

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
    string? Error = null)
{
    public static ScenarioListState Initial { get; } = new(ScenarioListStatus.Loading, []);
}
