using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using NetBench.Mobile.Tests.Infrastructure;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(TestApp))]
// Plumix-биндинги — синглтоны процесса: тесты с WidgetHost идут строго по одному.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NetBench.Mobile.Tests.Infrastructure;

/// <summary>
/// Замена MobileApp для headless-хоста: та же FluentTheme как источник базовых ресурсов,
/// но без PlumixApplication-бутстрапа — корневой виджет ставит <see cref="MobileHarness"/>.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            // Настоящая отрисовка Skia: без неё текст не измеряется, и раскладка экранов не та, что на устройстве
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont();
}
