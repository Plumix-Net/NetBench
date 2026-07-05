using NetBench.Core.Models;
using Xunit;

namespace NetBench.Core.Tests;

public class RequestResultTests
{
    [Fact]
    public void LatencyIsComputedFromTimestamps()
    {
        var result = new RequestResult(startNs: 1_000_000, endNs: 46_000_000, statusCode: 200, bytesReceived: 512, isError: false);

        Assert.Equal(45_000_000, result.LatencyNs);
        Assert.Equal(45.0, result.LatencyMs, precision: 6);
    }

    [Fact]
    public void ErrorFactoryMarksResultAsError()
    {
        var result = RequestResult.Error(startNs: 100, endNs: 200);

        Assert.True(result.IsError);
        Assert.Equal(0, result.StatusCode);
        Assert.Equal(0, result.BytesReceived);
        Assert.Equal(100, result.LatencyNs);
    }
}
