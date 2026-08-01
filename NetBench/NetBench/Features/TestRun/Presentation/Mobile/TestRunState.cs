using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.TestRun.Presentation.Mobile;

public enum TestRunPhase
{
    Running,
    Completed,
}

/// <summary>Иммутабельное состояние мобильного экрана прогона.</summary>
public sealed record TestRunState(
    TestRunPhase Phase,
    TestRunStats? Stats,
    IReadOnlyList<double> RpsPoints,
    TestRunReport? Report = null)
{
    public static TestRunState Initial { get; } = new(TestRunPhase.Running, null, []);
}
