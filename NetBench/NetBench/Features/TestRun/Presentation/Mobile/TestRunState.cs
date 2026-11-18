using NetBench.Features.TestRun.Domain;

namespace NetBench.Features.TestRun.Presentation.Mobile;

/// <summary>Прогон ещё идёт.</summary>
public sealed record RunInProgress;

/// <summary>Прогон завершён, отчёт готов.</summary>
public sealed record RunCompleted(TestRunReport Report);

/// <summary>Фаза прогона: отчёт существует только у завершённого.</summary>
public union TestRunPhase(RunInProgress, RunCompleted);

/// <summary>Иммутабельное состояние мобильного экрана прогона.</summary>
public sealed record TestRunState(
    TestRunPhase Phase,
    TestRunStats? Stats,
    IReadOnlyList<double> RpsPoints,
    bool StoppedInBackground = false)
{
    public static TestRunState Initial { get; } = new(new RunInProgress(), null, []);
}
