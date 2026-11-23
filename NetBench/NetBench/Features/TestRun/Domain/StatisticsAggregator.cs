using HdrHistogram;
using NetBench.Features.Scenarios.Domain;
using System.Collections.Concurrent;

namespace NetBench.Features.TestRun.Domain;

public sealed class StatisticsAggregator
{
    private static readonly int _errorKindCount = Enum.GetValues<RequestErrorKind>().Length;

    // Весь прогон и текущий интервал снапшота (сбрасывается в Snapshot) — под одним lock.
    private readonly LongHistogram _histogram = new(TimeStamp.Hours(1), 3);
    private readonly LongHistogram _intervalHistogram = new(TimeStamp.Hours(1), 3);
    private readonly Lock _histogramLock = new();

    private long _totalRequests;
    private long _respondedRequests;
    private long _errorCount;
    private long _totalBytes;
    private long _activeConnections;
    private long _activeUsers;
    private readonly ConcurrentDictionary<int, long> _statusCodes = new();

    private readonly long[] _errorKinds = new long[_errorKindCount];
    private readonly string?[] _errorMessages = new string?[_errorKindCount];

    private long _minLatencyNs = long.MaxValue;
    private long _maxLatencyNs;
    private long _sumLatencyNs;

    private readonly long _startTimestampNs;

    // Состояние предыдущего снапшота — для RPS за интервал. Пишется только из Snapshot (холодный путь).
    private long _lastSnapshotTotal;
    private long _lastSnapshotNs;
    private readonly Lock _snapshotLock = new();

    public StatisticsAggregator()
    {
        _startTimestampNs = MonotonicClock.NowNanoseconds();
        _lastSnapshotNs = _startTimestampNs;
    }

    public void Record(in RequestResult result)
    {
        Interlocked.Increment(ref _totalRequests);

        if (result.IsError)
            Interlocked.Increment(ref _errorCount);

        // Разбивка по кодам нужна и для ошибок: 4xx/5xx — реальные коды,
        // сетевые сбои — код 0 («нет ответа»).
        _statusCodes.AddOrUpdate(result.StatusCode, 1, static (_, c) => c + 1);

        if (!result.HasResponse)
        {
            // Без ответа сервера задержки нет: время до отказа в соединении (микросекунды)
            // утянуло бы перцентили к нулю и сделало бы их бессмысленными.
            Interlocked.Increment(ref _errorKinds[(int)result.ErrorKind]);
            return;
        }

        var latencyNs = result.LatencyNs;
        var latencyUs = Math.Max(1, latencyNs / 1000);

        lock (_histogramLock)
        {
            _histogram.RecordValue(latencyUs);
            _intervalHistogram.RecordValue(latencyUs);
        }

        Interlocked.Increment(ref _respondedRequests);
        Interlocked.Add(ref _totalBytes, result.BytesReceived);
        Interlocked.Add(ref _sumLatencyNs, latencyNs);

        InterlockedMin(ref _minLatencyNs, latencyNs);
        InterlockedMax(ref _maxLatencyNs, latencyNs);
    }

    /// <summary>Нужен ли ещё текст ошибки этого вида — чтобы движок не форматировал сообщения на каждом сбое.</summary>
    public bool NeedsErrorMessage(RequestErrorKind kind) => Volatile.Read(ref _errorMessages[(int)kind]) is null;

    /// <summary>Запоминает первый текст ошибки каждого вида.</summary>
    public void RecordErrorMessage(RequestErrorKind kind, string message) =>
        Interlocked.CompareExchange(ref _errorMessages[(int)kind], message, null);

    public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
    public void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);

    public void IncrementActiveUsers() => Interlocked.Increment(ref _activeUsers);
    public void DecrementActiveUsers() => Interlocked.Decrement(ref _activeUsers);

    // Хроника снапшотов для графика «задержка во времени» в отчёте.
    // Пишется ~4 раза/с из цикла статистики — это холодный путь.
    private readonly List<TestRunStats> _timeline = [];

    public TestRunStats Snapshot()
    {
        lock (_snapshotLock)
        {
            var total = Interlocked.Read(ref _totalRequests);
            var responded = Interlocked.Read(ref _respondedRequests);
            var errors = Interlocked.Read(ref _errorCount);
            var bytes = Interlocked.Read(ref _totalBytes);
            var active = Interlocked.Read(ref _activeConnections);
            var users = Interlocked.Read(ref _activeUsers);
            var sumNs = Interlocked.Read(ref _sumLatencyNs);
            var minNs = Interlocked.Read(ref _minLatencyNs);
            var maxNs = Interlocked.Read(ref _maxLatencyNs);

            var nowNs = MonotonicClock.NowNanoseconds();
            var elapsedSec = (nowNs - _startTimestampNs) / 1_000_000_000.0;
            var intervalSec = (nowNs - _lastSnapshotNs) / 1_000_000_000.0;
            var currentRps = intervalSec > 0 ? (total - _lastSnapshotTotal) / intervalSec : 0;
            _lastSnapshotTotal = total;
            _lastSnapshotNs = nowNs;

            double p50 = 0, p95 = 0, p99 = 0, intervalP95 = 0;
            lock (_histogramLock)
            {
                // histogram stores microseconds → convert to ms
                if (_histogram.TotalCount > 0)
                {
                    p50 = _histogram.GetValueAtPercentile(50.0) / 1000.0;
                    p95 = _histogram.GetValueAtPercentile(95.0) / 1000.0;
                    p99 = _histogram.GetValueAtPercentile(99.0) / 1000.0;
                }

                if (_intervalHistogram.TotalCount > 0)
                    intervalP95 = _intervalHistogram.GetValueAtPercentile(95.0) / 1000.0;
                _intervalHistogram.Reset();
            }

            var stats = new TestRunStats
            {
                TotalRequests = total,
                RespondedRequests = responded,
                ErrorCount = errors,
                TotalBytesReceived = bytes,
                ActiveConnections = (int)active,
                ActiveUsers = (int)users,
                Elapsed = TimeSpan.FromSeconds(elapsedSec),
                RequestsPerSecond = elapsedSec > 0 ? total / elapsedSec : 0,
                CurrentRequestsPerSecond = currentRps,
                LatencyP50Ms = p50,
                LatencyP95Ms = p95,
                LatencyP99Ms = p99,
                IntervalLatencyP95Ms = intervalP95,
                LatencyMinMs = responded > 0 ? minNs / 1_000_000.0 : 0,
                LatencyMaxMs = maxNs / 1_000_000.0,
                LatencyMeanMs = responded > 0 ? sumNs / 1_000_000.0 / responded : 0,
                TopError = TopError(),
            };

            lock (_timeline)
            {
                _timeline.Add(stats);
            }

            return stats;
        }
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
            Errors = ErrorBuckets(),
            Timeline = timeline,
        };
    }

    private List<ErrorBucket> ErrorBuckets()
    {
        var result = new List<ErrorBucket>();
        for (var kind = 1; kind < _errorKindCount; kind++)
        {
            var count = Interlocked.Read(ref _errorKinds[kind]);
            if (count > 0)
                result.Add(Bucket((RequestErrorKind)kind, count));
        }

        result.Sort(static (a, b) => b.Count.CompareTo(a.Count));
        return result;
    }

    private ErrorBucket? TopError()
    {
        var topKind = 0;
        long topCount = 0;
        for (var kind = 1; kind < _errorKindCount; kind++)
        {
            var count = Interlocked.Read(ref _errorKinds[kind]);
            if (count > topCount)
            {
                topKind = kind;
                topCount = count;
            }
        }

        return topCount > 0 ? Bucket((RequestErrorKind)topKind, topCount) : null;
    }

    private ErrorBucket Bucket(RequestErrorKind kind, long count) => new()
    {
        Kind = kind,
        Count = count,
        Message = Volatile.Read(ref _errorMessages[(int)kind]),
    };

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
