using Avalonia.Headless.XUnit;
using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Localization;
using NetBench.Mobile.Tests.Infrastructure;
using Plumix.Rendering;
using Xunit;

namespace NetBench.Mobile.Tests;

public sealed class ScenarioEditorTests
{
    private static Strings En => Strings.Instance.Root;

    [AvaloniaFact]
    public async Task NewScenario_Save_AddsCardToListAndRepository()
    {
        using var app = new MobileHarness();
        Assert.True(app.HasLabel(En.Mobile.EmptyHint));

        app.Tap(En.Scenarios.New);
        Assert.True(app.HasLabel(En.Mobile.NewScenarioTitle));
        var fields = TextFields(app);
        app.Tap(fields[0]);
        app.Type("Checkout API");
        app.Tap(fields[1]);
        app.Type("api.example.com/checkout");
        app.Tap("POST");
        app.Tap("25");
        app.Tap(En.Common.Save);

        // Домой, к новой карточке; хост показан без схемы
        await app.PumpUntilAsync(() => app.HasLabel("Checkout API"), "the new card appears");
        Assert.False(app.HasLabel(En.Mobile.EmptyHint));
        Assert.True(app.HasLabel("api.example.com/checkout"));

        var saved = Assert.Single(await new JsonScenarioRepository(ScenariosPath(app)).LoadAllAsync());
        Assert.Equal("Checkout API", saved.Name);
        Assert.Equal("https://api.example.com/checkout", saved.Target);
        Assert.Equal("POST", Assert.Single(saved.Requests).Method);
        Assert.Equal(25, saved.Load.ConcurrentUsers);
        Assert.False(saved.IsQuickTest);
    }

    [AvaloniaFact]
    public async Task Save_WithoutUrl_ShowsErrorAndKeepsEditorOpen()
    {
        using var app = new MobileHarness();

        app.Tap(En.Scenarios.New);
        app.Tap(TextFields(app)[0]);
        app.Type("No target");
        app.Tap(En.Common.Save);

        Assert.True(app.HasLabel(En.Mobile.NewScenarioTitle));
        Assert.Contains(app.Nodes(), node => node.Label.Contains(En.Shell.TargetRequired, StringComparison.Ordinal)
            || node.Hint.Contains(En.Shell.TargetRequired, StringComparison.Ordinal));
        Assert.Empty(await new JsonScenarioRepository(ScenariosPath(app)).LoadAllAsync());
    }

    private static List<SemanticsNode> TextFields(MobileHarness app) =>
        [.. app.Nodes().Where(node => node.Flags.HasFlag(SemanticsFlags.IsTextField)).OrderBy(node => node.GlobalRect.Y)];

    private static string ScenariosPath(MobileHarness app) => Path.Combine(app.DataDirectory, "scenarios");
}
