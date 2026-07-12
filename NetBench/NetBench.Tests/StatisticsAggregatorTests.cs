using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

public class StatisticsAggregatorTests
{
    private static RequestResult Ok(double latencyMs, int statusCode = 200, int bytes = 100) =>
        new(startNs: 0, endNs: (long)(latencyMs * 1_000_000), statusCode, bytes, isError: false);

    [Fact]
    public void SnapshotCountsTotalsErrorsAndBytes()
    {
        var aggregator = new StatisticsAggregator();
        aggregator.Record(Ok(10, bytes: 100));
        aggregator.Record(Ok(20, bytes: 200));
        aggregator.Record(RequestResult.Error(0, 5_000_000));

        var stats = aggregator.Snapshot();

        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(1, stats.ErrorCount);
        Assert.Equal(300, stats.TotalBytesReceived);
        Assert.Equal(1.0 / 3.0, stats.ErrorRate, precision: 9);
    }

    [Fact]
    public void SnapshotComputesMinMaxMeanLatency()
    {
        var aggregator = new StatisticsAggregator();
        aggregator.Record(Ok(10));
        aggregator.Record(Ok(30));
        aggregator.Record(Ok(50));

        var stats = aggregator.Snapshot();

        Assert.Equal(10.0, stats.LatencyMinMs, precision: 3);
        Assert.Equal(50.0, stats.LatencyMaxMs, precision: 3);
        Assert.Equal(30.0, stats.LatencyMeanMs, precision: 3);
    }

    [Fact]
    public void SnapshotComputesPercentilesFromHistogram()
    {
        var aggregator = new StatisticsAggregator();
        for (var i = 1; i <= 100; i++)
            aggregator.Record(Ok(i));

        var stats = aggregator.Snapshot();

        // HDR Histogram хранит микросекунды с точностью 3 значащих цифры —
        // сравниваем с допуском в 1 мс.
        Assert.InRange(stats.LatencyP50Ms, 49, 51);
        Assert.InRange(stats.LatencyP95Ms, 94, 96);
        Assert.InRange(stats.LatencyP99Ms, 98, 100);
    }

    [Fact]
    public void EmptyAggregatorProducesZeroedSnapshot()
    {
        var stats = new StatisticsAggregator().Snapshot();

        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0, stats.ErrorRate);
        Assert.Equal(0, stats.LatencyP50Ms);
        Assert.Equal(0, stats.LatencyMinMs);
        Assert.Equal(0, stats.LatencyMeanMs);
    }

    [Fact]
    public void BuildReportGroupsStatusCodes()
    {
        var aggregator = new StatisticsAggregator();
        aggregator.Record(Ok(10, statusCode: 200));
        aggregator.Record(Ok(10, statusCode: 200));
        aggregator.Record(new RequestResult(0, 10_000_000, statusCode: 404, bytesReceived: 0, isError: true));
        aggregator.Record(RequestResult.Error(0, 1_000_000)); // сетевой сбой → код 0

        var report = aggregator.BuildReport(new LoadScenario { Name = "test" }, DateTime.UtcNow);

        Assert.Equal(3, report.StatusCodes.Count);
        Assert.Equal(0, report.StatusCodes[0].StatusCode);
        Assert.Equal(1, report.StatusCodes[0].Count);
        Assert.Equal(200, report.StatusCodes[1].StatusCode);
        Assert.Equal(2, report.StatusCodes[1].Count);
        Assert.Equal(404, report.StatusCodes[2].StatusCode);
        Assert.Equal(1, report.StatusCodes[2].Count);
    }

    [Fact]
    public void BuildReportCollectsSnapshotTimeline()
    {
        var aggregator = new StatisticsAggregator();
        aggregator.Record(Ok(10));
        aggregator.Snapshot();
        aggregator.Record(Ok(20));
        aggregator.Snapshot();

        var report = aggregator.BuildReport(new LoadScenario { Name = "test" }, DateTime.UtcNow);

        // Два явных снапшота + финальный из BuildReport
        Assert.Equal(3, report.Timeline.Count);
        Assert.Equal(1, report.Timeline[0].TotalRequests);
        Assert.Equal(2, report.Timeline[1].TotalRequests);
        Assert.Equal(2, report.Timeline[2].TotalRequests);
    }

    [Fact]
    public async Task RecordIsThreadSafeUnderParallelLoad()
    {
        var aggregator = new StatisticsAggregator();
        const int threads = 8;
        const int perThread = 10_000;

        await Task.WhenAll(Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perThread; i++)
                aggregator.Record(Ok(5, bytes: 10));
        })));

        var stats = aggregator.Snapshot();

        Assert.Equal(threads * perThread, stats.TotalRequests);
        Assert.Equal(threads * perThread * 10L, stats.TotalBytesReceived);
        Assert.Equal(0, stats.ErrorCount);
    }
}
