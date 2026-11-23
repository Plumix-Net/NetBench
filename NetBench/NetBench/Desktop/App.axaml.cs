using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NetBench.Features.Shell.Presentation.Desktop;

namespace NetBench.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Своё меню приложения (на macOS — меню «NetBench»): без него Avalonia подставляет «About Avalonia».
        // Ставить именно здесь: Avalonia читает его сразу после Initialize, до OnFrameworkInitializationCompleted.
        NativeMenu.SetMenu(this, Views.MainMenu.CreateApplicationMenu(() => Views.MainMenu.ShowAbout(
            (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow)));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var composition = new Composition.Composition(Composition.DesktopStorage.Default);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new Views.MainWindow
            {
                DataContext = composition.Root,
            };
            desktop.MainWindow = window;

            Views.MainMenu.Attach(window, composition.Root);
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new ShellView
            {
                DataContext = composition.Root,
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new ShellView
            {
                DataContext = composition.Root,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
