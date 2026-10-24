using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;

namespace NetBench.Screenshots;

/// <summary>Сценарии и история прогонов, с которыми приложение открывается на скриншотах.</summary>
internal sealed class DemoData
{
    /// <summary>Стенд из заметок для App Review: адрес в локальной сети, обслуживает <see cref="DemoTarget"/>.</summary>
    public const string LanHost = "http://192.168.1.10:8080";

    public required LoadScenario Checkout { get; init; }

    public required IReadOnlyList<LoadScenario> Scenarios { get; init; }

    public required IReadOnlyList<TestRunReport> History { get; init; }

    public static DemoData For(string culture)
    {
        var ru = culture.StartsWith("ru", StringComparison.Ordinal);

        var checkout = Scenario(ru ? "Оформление заказа" : "Checkout API", $"{LanHost}/api/orders", users: 50, seconds: 20);
        var catalog = Scenario(ru ? "Каталог товаров" : "Product catalog", "https://staging.shop.dev/api/products", 25, 60);
        var search = Scenario(ru ? "Поиск" : "Search", "https://staging.shop.dev/api/search?q=sneakers", 10, 30);
        var auth = Scenario(ru ? "Авторизация" : "Auth service", "https://auth.staging.shop.dev/token", 10, 30);
        var quick = new LoadScenario
        {
            Name = "Quick test",
            Target = $"{LanHost}/health",
            IsQuickTest = true,
            Load = new LoadConfig { ConcurrentUsers = 10, Duration = TimeSpan.FromSeconds(15) },
        };

        var now = DateTime.UtcNow;
        return new DemoData
        {
            Checkout = checkout,
            Scenarios = [checkout, catalog, search, auth],
            History =
            [
                Report(catalog, now.AddMinutes(-18), rps: 612, p50: 38, p95: 71, errors: 0.000),
                Report(auth, now.AddMinutes(-52), rps: 214, p50: 44, p95: 96, errors: 0.002),
                Report(quick, now.AddHours(-3), rps: 388, p50: 21, p95: 34, errors: 0.000),
                Report(search, now.AddHours(-26), rps: 141, p50: 62, p95: 188, errors: 0.071),
            ],
        };
    }

    private static LoadScenario Scenario(string name, string target, int users, int seconds) => new()
    {
        Name = name,
        Target = target,
        Load = new LoadConfig { ConcurrentUsers = users, Duration = TimeSpan.FromSeconds(seconds) },
    };

    private static TestRunReport Report(LoadScenario scenario, DateTime finishedAt, double rps, double p50, double p95, double errors)
    {
        var duration = scenario.Load.Duration;
        var total = (long)(rps * duration.TotalSeconds);
        var failed = (long)(total * errors);

        TestRunStats Stats(double factor, TimeSpan elapsed) => new()
        {
            RequestsPerSecond = rps * factor,
            TotalRequests = (long)(total * elapsed / duration),
            ErrorCount = (long)(failed * elapsed / duration),
            LatencyMinMs = p50 * 0.45,
            LatencyP50Ms = p50,
            LatencyMeanMs = p50 * 1.12,
            LatencyP95Ms = p95 * (2 - factor),
            LatencyP99Ms = p95 * 1.6,
            LatencyMaxMs = p95 * 3.1,
            Elapsed = elapsed,
        };

        var timeline = new List<TestRunStats>();
        for (var tick = 1; tick <= duration.TotalSeconds * 4; tick++)
        {
            var wave = 0.92 + 0.08 * Math.Sin(tick / 3.0) + (tick < 8 ? -0.05 * (8 - tick) : 0);
            timeline.Add(Stats(wave, TimeSpan.FromMilliseconds(250 * tick)));
        }

        return new TestRunReport
        {
            Scenario = scenario,
            StartedAt = finishedAt - duration,
            FinishedAt = finishedAt,
            Summary = Stats(1, duration),
            StatusCodes = failed == 0
                ? [new StatusCodeBucket { StatusCode = 200, Count = total }]
                : [new StatusCodeBucket { StatusCode = 200, Count = total - failed }, new StatusCodeBucket { StatusCode = 503, Count = failed }],
            Timeline = timeline,
        };
    }
}
