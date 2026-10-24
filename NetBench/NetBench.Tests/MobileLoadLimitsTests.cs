using NetBench.Features.Scenarios.Domain;
using Xunit;

namespace NetBench.Tests;

public class MobileLoadLimitsTests
{
    [Fact]
    public void PresetsStayWithinLimits()
    {
        Assert.All(MobileLoadLimits.UserPresets, users => Assert.InRange(users, 1, MobileLoadLimits.MaxUsers));
        Assert.All(
            MobileLoadLimits.DurationPresets,
            duration => Assert.InRange(duration, TimeSpan.FromSeconds(1), MobileLoadLimits.MaxDuration));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 10)]
    [InlineData(500, MobileLoadLimits.MaxUsers)]
    public void ClampsUsers(int requested, int expected) =>
        Assert.Equal(expected, MobileLoadLimits.Clamp(new LoadConfig { ConcurrentUsers = requested }).ConcurrentUsers);

    [Fact]
    public void ClampsDurationAndRampUp()
    {
        var load = MobileLoadLimits.Clamp(new LoadConfig
        {
            Duration = TimeSpan.FromHours(1),
            RampUp = TimeSpan.FromHours(2),
            ThinkTime = TimeSpan.FromMilliseconds(250),
        });

        Assert.Equal(MobileLoadLimits.MaxDuration, load.Duration);
        Assert.Equal(MobileLoadLimits.MaxDuration, load.RampUp);
        Assert.Equal(TimeSpan.FromMilliseconds(250), load.ThinkTime);
    }

    [Fact]
    public void ZeroDurationFallsBackToMaximum() =>
        Assert.Equal(
            MobileLoadLimits.MaxDuration,
            MobileLoadLimits.Clamp(new LoadConfig { Duration = TimeSpan.Zero }).Duration);

    [Fact]
    public void ApplyKeepsIdentityAndRequests()
    {
        var scenario = new LoadScenario
        {
            Name = "Checkout",
            Target = "https://example.com",
            IsQuickTest = true,
            Load = new LoadConfig { ConcurrentUsers = 200 },
        };
        scenario.Requests.Add(new RequestStep { Method = "POST", Path = "/cart" });

        var limited = MobileLoadLimits.Apply(scenario);

        Assert.Equal(scenario.Id, limited.Id);
        Assert.Equal("Checkout", limited.Name);
        Assert.True(limited.IsQuickTest);
        Assert.Same(scenario.Requests, limited.Requests);
        Assert.Equal(MobileLoadLimits.MaxUsers, limited.Load.ConcurrentUsers);
        Assert.Equal(200, scenario.Load.ConcurrentUsers);
    }
}
