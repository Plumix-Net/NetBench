using System.Diagnostics;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

public class MonotonicClockTests
{
    [Fact]
    public void NowNanoseconds_IsPositiveAndMonotonic()
    {
        var first = MonotonicClock.NowNanoseconds();
        var second = MonotonicClock.NowNanoseconds();

        Assert.True(first > 0);
        Assert.True(second >= first);
    }

    [Fact]
    public void ToNanoseconds_DoesNotOverflowForLargeUptime()
    {
        // Год аптайма в тиках текущей платформы: наивное ts * 1e9 здесь давно бы переполнилось
        var yearOfTicks = Stopwatch.Frequency * 60L * 60L * 24L * 365L;

        var ns = MonotonicClock.ToNanoseconds(yearOfTicks);

        Assert.Equal(1_000_000_000L * 60L * 60L * 24L * 365L, ns);
    }

    [Fact]
    public void ToNanoseconds_PreservesSubsecondResolution()
    {
        // Полсекунды в тиках → ровно 5e8 нс независимо от Frequency платформы
        var halfSecondTicks = Stopwatch.Frequency / 2;

        var ns = MonotonicClock.ToNanoseconds(halfSecondTicks);

        Assert.InRange(ns, 499_999_999L, 500_000_001L);
    }
}
