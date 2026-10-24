using Avalonia.Headless.XUnit;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using NetBench.Mobile.Tests.Infrastructure;
using Xunit;

namespace NetBench.Mobile.Tests;

public sealed class QuickTestScreenTests
{
    private static Strings En => Strings.Instance.Root;

    private static IEnumerable<string> UserLabels =>
        MobileLoadLimits.UserPresets.Select(users => users.ToString(DisplayCulture.Current));

    private static IEnumerable<string> DurationLabels => MobileLoadLimits.DurationPresets.Select(En.DurationLabel);

    [AvaloniaFact]
    public void Presets_OfferOnlyLoadWithinMobileLimits()
    {
        using var app = new MobileHarness();

        app.OpenQuickTest("127.0.0.1");

        // Ровно пресеты из MobileLoadLimits — свободного ввода больше лимита нет
        var users = app.Presets(UserLabels);
        var durations = app.Presets(DurationLabels);
        Assert.All(MobileLoadLimits.UserPresets, value => Assert.InRange(value, 1, MobileLoadLimits.MaxUsers));
        Assert.All(MobileLoadLimits.DurationPresets, value => Assert.True(value <= MobileLoadLimits.MaxDuration));

        // Значения по умолчанию — из LoadConfig, выбран ровно один пресет в каждом ряду
        var defaults = new LoadConfig();
        Assert.Single(users, node => node.IsSelected());
        Assert.True(users[IndexOf(MobileLoadLimits.UserPresets, defaults.ConcurrentUsers)].IsSelected());
        Assert.Single(durations, node => node.IsSelected());
        Assert.True(durations[IndexOf(MobileLoadLimits.DurationPresets, defaults.Duration)].IsSelected());
    }

    [AvaloniaFact]
    public void Preset_Tap_MovesSelectionWithinTheRow()
    {
        using var app = new MobileHarness();
        app.OpenQuickTest("127.0.0.1");

        app.Tap(app.Presets(UserLabels)[^1]);
        app.Tap(app.Presets(DurationLabels)[^1]);

        var users = app.Presets(UserLabels);
        var durations = app.Presets(DurationLabels);
        Assert.Single(users, node => node.IsSelected());
        Assert.True(users[^1].IsSelected());
        Assert.Single(durations, node => node.IsSelected());
        Assert.True(durations[^1].IsSelected());
    }

    [AvaloniaFact]
    public async Task Run_WithLargestPresets_StartsWithinMobileLimits()
    {
        using var target = new LocalTarget();
        using var app = new MobileHarness(new AppSettings { OwnTargetsConfirmed = true });
        app.OpenQuickTest(target.Url);

        app.Tap(app.Presets(UserLabels)[^1]);
        app.Tap(app.Presets(DurationLabels)[^1]);
        app.Tap(En.Mobile.Run);
        await app.WaitForMonitorAsync();
        await app.StopRunAsync();

        var report = Assert.Single(app.ReadHistory());
        Assert.True(report.Scenario.IsQuickTest);
        Assert.Equal(target.Url, report.Scenario.Target);
        Assert.Equal(MobileLoadLimits.MaxUsers, report.Scenario.Load.ConcurrentUsers);
        Assert.Equal(MobileLoadLimits.MaxDuration, report.Scenario.Load.Duration);
    }

    private static int IndexOf<T>(IReadOnlyList<T> values, T value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], value))
                return i;
        }

        throw new InvalidOperationException($"{value} is not a preset");
    }
}
