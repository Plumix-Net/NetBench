using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace NetBench.Android;

[Activity(
    Label = "NetBench.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
    // Plumix отдаёт всё приложение одним общим MainView, а Android уничтожает Activity
    // после выхода «назад» с задержкой (~3 с). Запуск в это окно создаёт новую Activity,
    // которая вставляет тот же view, ещё висящий в старой, — и падает с
    // «already has a visual parent». Поэтому новая Activity забирает view у уходящей.
    private static MainActivity? _current;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        if (_current is { } previous && previous != this)
            previous.Content = null;

        _current = this;
        base.OnCreate(savedInstanceState);

        BackRequested += OnBackRequested;
    }

    protected override void OnDestroy()
    {
        BackRequested -= OnBackRequested;

        if (_current == this)
            _current = null;

        base.OnDestroy();
    }

    // Системный «назад» Avalonia отдаёт событием BackRequested (OnBackPressedDispatcher),
    // а Plumix-хост ловит только клавишу Back — без этого моста навигатор и PopScope
    // его не видят, и любой «назад» закрывает Activity. Если навигатору некуда
    // возвращаться (главный экран), событие не помечаем — Android закроет приложение сам.
    private static void OnBackRequested(object? sender, AndroidBackRequestedEventArgs e) =>
        e.Handled = Plumix.Widgets.Navigator.TryHandleBackButton();
}
