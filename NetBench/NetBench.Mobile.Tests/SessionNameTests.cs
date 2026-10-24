using Avalonia.Headless.XUnit;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using NetBench.Mobile.Tests.Infrastructure;
using Xunit;

namespace NetBench.Mobile.Tests;

public sealed class SessionNameTests
{
    [AvaloniaFact]
    public async Task QuickTestSession_IsNamedInCurrentLanguage_NotTheOneItRanIn()
    {
        using var target = new LocalTarget();
        using var app = new MobileHarness(new AppSettings { OwnTargetsConfirmed = true });
        var en = Strings.Instance.Root;

        app.OpenQuickTest(target.Url);
        app.Tap(en.Mobile.Run);
        await app.WaitForMonitorAsync();
        await app.StopRunAsync();
        app.Tap(en.Mobile.Back);

        var session = app.GetByLabel(en.Mobile.RecentSessions.ToUpperInvariant());
        Assert.Contains(app.Nodes(), node => IsSessionRow(node, en.Mobile.QuickTest, session));

        // Язык меняют в настройках — как пользователь
        app.Tap(en.Settings.OpenSettings);
        app.Tap("Русский");
        Assert.Equal("ru-RU", app.ReadSettings().Culture);
        var ru = Strings.Instance.Root;
        Assert.NotEqual(en.Mobile.QuickTest, ru.Mobile.QuickTest);
        app.Tap(ru.Mobile.Back);

        // Отчёт хранит имя на языке запуска, а строка истории подписана на текущем
        Assert.Equal(en.Mobile.QuickTest, Assert.Single(app.ReadHistory()).Scenario.Name);
        session = app.GetByLabel(ru.Mobile.RecentSessions.ToUpperInvariant());
        Assert.Contains(app.Nodes(), node => IsSessionRow(node, ru.Mobile.QuickTest, session));
        Assert.DoesNotContain(app.Nodes(), node => IsSessionRow(node, en.Mobile.QuickTest, session));
    }

    /// <summary>Строка истории — под заголовком «Последние сессии», с именем сценария в подписи.</summary>
    private static bool IsSessionRow(Plumix.Rendering.SemanticsNode node, string name, Plumix.Rendering.SemanticsNode header) =>
        node.GlobalRect.Y > header.GlobalRect.Y && node.Label.Split('\n').Contains(name);
}
