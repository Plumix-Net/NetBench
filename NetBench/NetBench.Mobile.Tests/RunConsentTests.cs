using Avalonia.Headless.XUnit;
using NetBench.Localization;
using NetBench.Mobile.Tests.Infrastructure;
using Xunit;

namespace NetBench.Mobile.Tests;

public sealed class RunConsentTests
{
    private static Strings En => Strings.Instance.Root;

    [AvaloniaFact]
    public async Task FirstRun_AsksToTestOnlyOwnServers_Once()
    {
        using var target = new LocalTarget();
        using var app = new MobileHarness();
        Assert.False(app.ReadSettings().OwnTargetsConfirmed);

        app.OpenQuickTest(target.Url);
        app.Tap(En.Mobile.Run);
        Assert.True(app.HasLabel(En.Mobile.OwnTargetsTitle));

        // «Отмена» — прогона нет, согласие не запомнено
        app.Tap(En.Common.Cancel);
        Assert.False(app.HasLabel(En.Mobile.OwnTargetsTitle));
        Assert.False(app.HasLabel(En.Mobile.Stop));
        Assert.False(app.ReadSettings().OwnTargetsConfirmed);

        app.Tap(En.Mobile.Run);
        Assert.True(app.HasLabel(En.Mobile.OwnTargetsTitle));
        app.Tap(En.Mobile.OwnTargetsConfirm);
        await app.WaitForMonitorAsync();
        // Согласие на диске — переживёт перезапуск
        Assert.True(app.ReadSettings().OwnTargetsConfirmed);
        await app.StopRunAsync();

        // Второй прогон стартует сразу, без диалога
        app.Tap(En.Mobile.Back);
        app.OpenQuickTest(target.Url);
        app.Tap(En.Mobile.Run);
        Assert.False(app.HasLabel(En.Mobile.OwnTargetsTitle));
        await app.WaitForMonitorAsync();
        await app.StopRunAsync();
    }
}
