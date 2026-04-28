namespace NetBench.Core.Models;

public sealed class LoadScenario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public List<RequestStep> Requests { get; init; } = [];
    public LoadConfig Load { get; init; } = new();
}

public sealed class RequestStep
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public Dictionary<string, string> Headers { get; init; } = [];
    public string? Body { get; set; }
}

public sealed class LoadConfig
{
    public int ConcurrentUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RampUp { get; set; } = TimeSpan.Zero;
    public TimeSpan ThinkTime { get; set; } = TimeSpan.Zero;
}
