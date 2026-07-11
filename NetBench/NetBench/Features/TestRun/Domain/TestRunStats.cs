using NetBench.Features.Scenarios.Domain;
namespace NetBench.Features.TestRun.Domain;

public sealed class TestRunStats
{
    public double RequestsPerSecond { get; init; }
    public long TotalRequests { get; init; }
    public long ErrorCount { get; init; }
    public double ErrorRate => TotalRequests == 0 ? 0 : (double)ErrorCount / TotalRequests;

    public double LatencyP50Ms { get; init; }
    public double LatencyP95Ms { get; init; }
    public double LatencyP99Ms { get; init; }
    public double LatencyMinMs { get; init; }
    public double LatencyMaxMs { get; init; }
    public double LatencyMeanMs { get; init; }

    public long TotalBytesReceived { get; init; }
    public int ActiveConnections { get; init; }
    public TimeSpan Elapsed { get; init; }
}

public sealed class TestRunReport
{
    public LoadScenario Scenario { get; init; } = null!;
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; init; }
    public TestRunStats Summary { get; init; } = null!;
    public IReadOnlyList<StatusCodeBucket> StatusCodes { get; init; } = [];
}

public sealed class StatusCodeBucket
{
    public int StatusCode { get; init; }
    public long Count { get; init; }
}
