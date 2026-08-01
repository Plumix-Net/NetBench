using HdrHistogram;
using NetBench.Features.Scenarios.Domain;
using System.Collections.Concurrent;

namespace NetBench.Features.TestRun.Domain;

public sealed class StatisticsAggregator
{
    private readonly LongHistogram _histogram = new(TimeStamp.Hours(1), 3);
    private long _totalRequests;
    private long _errorCount;
    private long _totalBytes;
    private long _activeConnections;
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();

    private long _minLatencyNs = long.MaxValue;
    private long _maxLatencyNs;
    private long _sumLatencyNs;

    private readonly long _startTimestampNs;

    public StatisticsAggregator()
    {
        _startTimestampNs = MonotonicClock.NowNanoseconds();
    }

    public void Record(in RequestResult result)
    {
        var latencyNs = result.LatencyNs;

        lock (_histogram)
        {
            _histogram.RecordValue(Math.Max(1, latencyNs / 1000)); // microseconds
        }

        Interlocked.Increment(ref _totalRequests);
        Interlocked.Add(ref _totalBytes, result.BytesReceived);
        Interlocked.Add(ref _sumLatencyNs, latencyNs);

        if (result.IsError)
            Interlocked.Increment(ref _errorCount);

        // Разбивка по кодам нужна и для ошибок: 4xx/5xx — реальные коды,
        // сетевые сбои — код 0 («нет ответа»).
        _statusCodes.AddOrUpdate(result.StatusCode, 1, (_, c) => c + 1);

        InterlockedMin(ref _minLatencyNs, latencyNs);
        InterlockedMax(ref _maxLatencyNs, latencyNs);
    }

    public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
    public void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);

    // Хроника снапшотов для графика «задержка во времени» в отчёте.
    // Пишется ~4 раза/с из цикла статистики — это холодный путь.
    private readonly List<TestRunStats> _timeline = [];

    public TestRunStats Snapshot()
    {
        var total = Interlocked.Read(ref _totalRequests);
        var errors = Interlocked.Read(ref _errorCount);
        var bytes = Interlocked.Read(ref _totalBytes);
        var active = Interlocked.Read(ref _activeConnections);
        var sumNs = Interlocked.Read(ref _sumLatencyNs);
        var minNs = Interlocked.Read(ref _minLatencyNs);
        var maxNs = Interlocked.Read(ref _maxLatencyNs);

        var elapsedNs = MonotonicClock.NowNanoseconds() - _startTimestampNs;
        var elapsedSec = elapsedNs / 1_000_000_000.0;

        double p50 = 0, p95 = 0, p99 = 0;
        lock (_histogram)
        {
            if (_histogram.TotalCount > 0)
            {
                // histogram stores microseconds → convert to ms
                p50 = _histogram.GetValueAtPercentile(50.0) / 1000.0;
                p95 = _histogram.GetValueAtPercentile(95.0) / 1000.0;
                p99 = _histogram.GetValueAtPercentile(99.0) / 1000.0;
            }
        }

        var stats = new TestRunStats
        {
            TotalRequests = total,
            ErrorCount = errors,
            TotalBytesReceived = bytes,
            ActiveConnections = (int)active,
            Elapsed = TimeSpan.FromSeconds(elapsedSec),
            RequestsPerSecond = elapsedSec > 0 ? total / elapsedSec : 0,
            LatencyP50Ms = p50,
            LatencyP95Ms = p95,
            LatencyP99Ms = p99,
            LatencyMinMs = total > 0 ? minNs / 1_000_000.0 : 0,
            LatencyMaxMs = maxNs / 1_000_000.0,
            LatencyMeanMs = total > 0 ? sumNs / 1_000_000.0 / total : 0,
        };

        lock (_timeline)
        {
            _timeline.Add(stats);
        }

        return stats;
    }

    public TestRunReport BuildReport(LoadScenario scenario, DateTime startedAt)
    {
        var summary = Snapshot();
        var buckets = _statusCodes
            .Select(kv => new StatusCodeBucket { StatusCode = kv.Key, Count = kv.Value })
            .OrderBy(b => b.StatusCode)
            .ToList();

        TestRunStats[] timeline;
        lock (_timeline)
        {
            timeline = [.. _timeline];
        }

        return new TestRunReport
        {
            Scenario = scenario,
            StartedAt = startedAt,
            FinishedAt = DateTime.UtcNow,
            Summary = summary,
            StatusCodes = buckets,
            Timeline = timeline,
        };
    }


    private static void InterlockedMin(ref long location, long value)
    {
        long current;
        do
        {
            current = Interlocked.Read(ref location);
            if (value >= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    private static void InterlockedMax(ref long location, long value)
    {
        long current;
        do
        {
            current = Interlocked.Read(ref location);
            if (value <= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
