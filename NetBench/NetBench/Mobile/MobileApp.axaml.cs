using Avalonia.Markup.Xaml;
using NetBench.Features.Shell.Presentation.Mobile;
using Plumix;
using Plumix.Widgets;

namespace NetBench.Mobile;

/// <summary>
/// Мобильная версия приложения: UI на Plumix-виджетах вместо Avalonia-контролов.
/// Её бутстрапят Android/iOS-головы; desktop продолжает использовать Avalonia-версию (App).
/// </summary>
public partial class MobileApp : PlumixApplication
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    protected override Widget CreateRootWidget() => new MobileShell();

    protected override PlumixOptions CreateOptions() => new()
    {
        Title = "NetBench",
        InitialWindowSize = new Avalonia.Size(390, 780),
    };
}
