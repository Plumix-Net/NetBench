using Avalonia.Headless.XUnit;
using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using NetBench.Mobile.Tests.Infrastructure;
using Plumix.Rendering;
using Xunit;

namespace NetBench.Mobile.Tests;

/// <summary>
/// То, что слышит TalkBack/VoiceOver: у всего нажимаемого есть имя и роль, выбор объявляется,
/// жест «свайп — удалить» продублирован действием. И крупный шрифт не уводит главные кнопки за экран.
/// </summary>
public sealed class AccessibilityTests
{
    private static Strings En => Strings.Instance.Root;

    [AvaloniaFact]
    public async Task EveryScreen_TappableNodesHaveNameAndRole()
    {
        using var target = new LocalTarget();
        using var app = new MobileHarness(new AppSettings { OwnTargetsConfirmed = true });

        app.OpenQuickTest(target.Url);
        AssertTappablesNamed(app, "quick test");
        app.Tap(En.Mobile.Run);
        await app.WaitForMonitorAsync();
        AssertTappablesNamed(app, "monitor");
        await app.StopRunAsync();
        AssertTappablesNamed(app, "result");
        app.Tap(En.Mobile.Back);

        app.Tap(En.Scenarios.New);
        AssertTappablesNamed(app, "editor");
        // Поля находятся по своим подписям — значит, TalkBack их и прочтёт
        app.Tap(En.Scenarios.Editor.Name);
        app.Type("Checkout");
        app.Tap(En.Mobile.TargetUrl);
        app.Type("api.example.com");
        app.Tap(En.Common.Save);
        await app.PumpUntilAsync(() => app.HasLabel("Checkout"), "the new card appears");
        AssertTappablesNamed(app, "home");

        app.Tap(En.Settings.OpenSettings);
        AssertTappablesNamed(app, "settings");
        app.Tap(En.Settings.About);
        AssertTappablesNamed(app, "about");
    }

    [AvaloniaFact]
    public void ScreenTitlesAndSections_AreHeadings()
    {
        using var app = new MobileHarness();
        Assert.True(app.GetByLabel(En.App.Title).Flags.HasFlag(SemanticsFlags.IsHeader));

        app.Tap(En.Settings.OpenSettings);
        Assert.True(app.GetByLabel(En.Settings.Title).Flags.HasFlag(SemanticsFlags.IsHeader));
        Assert.True(app.GetByLabel(En.Settings.Language.ToUpperInvariant()).Flags.HasFlag(SemanticsFlags.IsHeader));
    }

    [AvaloniaFact]
    public void SettingsOptions_AnnounceSelectedState()
    {
        using var app = new MobileHarness();
        app.Tap(En.Settings.OpenSettings);

        // «Как в системе» есть и в языке, и в теме — берём первую группу (язык)
        var system = app.Nodes().First(node => node.Label == En.Settings.LanguageSystem);
        Assert.True(system.Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.True(system.Flags.HasFlag(SemanticsFlags.IsInMutuallyExclusiveGroup));
        Assert.False(app.GetByLabel("English").Flags.HasFlag(SemanticsFlags.IsSelected));

        app.Tap("English");

        Assert.True(app.GetByLabel("English").Flags.HasFlag(SemanticsFlags.IsSelected));
        Assert.False(app.Nodes().First(node => node.Label == En.Settings.LanguageSystem)
            .Flags.HasFlag(SemanticsFlags.IsSelected));
    }

    [AvaloniaFact]
    public async Task ScenarioCard_DeleteIsAvailableWithoutSwipe()
    {
        var directory = Directory.CreateTempSubdirectory("netbench-mobile-tests-").FullName;
        var repository = new JsonScenarioRepository(Path.Combine(directory, "scenarios"));
        await repository.SaveAsync(new LoadScenario { Name = "Seeded API", Target = "https://api.example.com" });
        using var app = new MobileHarness(dataDirectory: directory);
        await app.PumpUntilAsync(() => app.HasLabel("Seeded API"), "the seeded card appears");

        app.PerformCustomAction(app.GetByLabel("Seeded API"), En.Mobile.Delete);
        Assert.True(app.HasLabel(En.Mobile.DeleteScenarioTitle));
        app.Tap(En.Mobile.Delete);

        await app.PumpUntilAsync(() => !app.HasLabel("Seeded API"), "the card disappears");
        Assert.Empty(await repository.LoadAllAsync());
    }

    [AvaloniaFact]
    public async Task LargeFont_KeepsPrimaryActionsOnScreen()
    {
        using var target = new LocalTarget();
        using var app = new MobileHarness(new AppSettings { OwnTargetsConfirmed = true }, textScale: 2.0);

        AssertOnScreen(app, En.Mobile.QuickTest, En.Scenarios.New, En.Settings.OpenSettings, En.Mobile.ToggleTheme);

        app.OpenQuickTest(target.Url);
        AssertOnScreen(app, [En.Mobile.Run, .. MobileLoadLimits.DurationPresets.Select(En.DurationLabel)]);

        app.Tap(En.Mobile.Run);
        await app.WaitForMonitorAsync();
        AssertOnScreen(app, En.Mobile.Stop);
        await app.StopRunAsync();
        app.Tap(En.Mobile.Back);

        app.Tap(En.Scenarios.New);
        AssertOnScreen(app, En.Common.Save, "DELETE");
    }

    private static void AssertOnScreen(MobileHarness app, params string[] labels)
    {
        foreach (var label in labels)
            Assert.True(app.IsOnScreen(app.GetByLabel(label)), $"“{label}” is not fully on screen");
    }

    /// <summary>
    /// Нажимаемый узел без имени TalkBack читает как «кнопка без подписи», без роли — как текст,
    /// который непонятно что делает. Подложка поля ввода (узел над IsTextField) роль берёт от поля.
    /// </summary>
    private static void AssertTappablesNamed(MobileHarness app, string screen)
    {
        foreach (var node in app.Nodes().Where(static node => node.Actions.HasFlag(SemanticsActions.Tap) && !node.IsHidden))
        {
            var where = $"{screen}: node {node.Id} at {node.GlobalRect}";
            Assert.False(node.Label.Length == 0 && node.Tooltip.Length == 0, $"{where} has no label");
            Assert.True(
                node.Flags.HasFlag(SemanticsFlags.IsButton)
                || node.Flags.HasFlag(SemanticsFlags.IsLink)
                || node.Flags.HasFlag(SemanticsFlags.IsTextField)
                || HasTextFieldChild(node),
                $"{where} “{node.Label}” has no role");
        }
    }

    private static bool HasTextFieldChild(SemanticsNode node)
    {
        var found = false;
        node.VisitChildren(child =>
        {
            found |= child.Flags.HasFlag(SemanticsFlags.IsTextField);
            return !found;
        });
        return found;
    }
}
