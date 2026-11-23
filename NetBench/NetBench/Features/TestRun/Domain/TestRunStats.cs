using NetBench.Features.Scenarios.Domain;
namespace NetBench.Features.TestRun.Domain;

public sealed class TestRunStats
{
    /// <summary>Средний RPS с начала прогона — итог для отчёта.</summary>
    public double RequestsPerSecond { get; init; }

    /// <summary>RPS за последний интервал снапшота (~250 мс) — живая цифра и график во время прогона.</summary>
    public double CurrentRequestsPerSecond { get; init; }

    public long TotalRequests { get; init; }
    public long ErrorCount { get; init; }
    public double ErrorRate => TotalRequests == 0 ? 0 : (double)ErrorCount / TotalRequests;

    /// <summary>
    /// Запросы, на которые сервер ответил (любым кодом). Задержки и перцентили считаются только по ним:
    /// время «до ошибки соединения» — не время ответа сервера.
    /// </summary>
    public long RespondedRequests { get; init; }

    public double LatencyP50Ms { get; init; }
    public double LatencyP95Ms { get; init; }
    public double LatencyP99Ms { get; init; }
    public double LatencyMinMs { get; init; }
    public double LatencyMaxMs { get; init; }
    public double LatencyMeanMs { get; init; }

    /// <summary>P95 ответов за последний интервал снапшота; 0 — ответов в интервале не было.</summary>
    public double IntervalLatencyP95Ms { get; init; }

    public long TotalBytesReceived { get; init; }

    /// <summary>Запросы в полёте прямо сейчас.</summary>
    public int ActiveConnections { get; init; }

    /// <summary>Запущенные виртуальные пользователи — растут во время ramp-up.</summary>
    public int ActiveUsers { get; init; }

    public TimeSpan Elapsed { get; init; }

    /// <summary>Самая частая причина запросов без ответа; null — таких не было.</summary>
    public ErrorBucket? TopError { get; init; }
}

public sealed class TestRunReport
{
    public LoadScenario Scenario { get; init; } = null!;
    public DateTime StartedAt { get; init; }
    public DateTime FinishedAt { get; init; }
    public TestRunStats Summary { get; init; } = null!;
    public IReadOnlyList<StatusCodeBucket> StatusCodes { get; init; } = [];

    /// <summary>Запросы без ответа по причинам, частые первыми.</summary>
    public IReadOnlyList<ErrorBucket> Errors { get; init; } = [];

    /// <summary>Снапшоты статистики по ходу прогона (~4/с) — для графиков в отчёте.</summary>
    public IReadOnlyList<TestRunStats> Timeline { get; init; } = [];
}

public sealed class StatusCodeBucket
{
    public int StatusCode { get; init; }
    public long Count { get; init; }
}

public sealed class ErrorBucket
{
    public RequestErrorKind Kind { get; init; }
    public long Count { get; init; }

    /// <summary>Текст первого исключения этого вида — подробность для пользователя (может быть пустым).</summary>
    public string? Message { get; init; }
}
