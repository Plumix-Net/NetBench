using Android.Content.PM;
using NetBench.Features.Shell.Presentation.Mobile;
using NetBench.Mobile.Platform;
using Plumix.Android;
using Plumix.Widgets;

namespace NetBench.Android;

// PlumixActivity — нативный хост Plumix без Avalonia (рендер Impeller): передаёт фреймворку жизненный цикл,
// восстановление, системный «назад» (включая predictive back на Android 14+), ввод и доступность — своих мостов
// в голове не нужно. Только портрет: вёрстка экранов рассчитана на телефон в вертикальной ориентации.
// Изменения конфигурации (тема, язык, масштаб шрифта, клавиатура) хост обрабатывает сам, без пересоздания Activity —
// набор как в шаблоне Flutter.
[Activity(
    Label = "NetBench",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/ic_launcher",
    RoundIcon = "@mipmap/ic_launcher_round",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.Keyboard
                           | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Locale
                           | ConfigChanges.LayoutDirection | ConfigChanges.FontScale | ConfigChanges.ScreenLayout
                           | ConfigChanges.Density | ConfigChanges.UiMode)]
public class MainActivity : PlumixActivity
{
    private static WeakReference<MainActivity>? _current;

    /// <summary>Текущая Activity — для Intent'ов и флагов окна из <see cref="AndroidPlatformServices"/>.</summary>
    internal static MainActivity? Current => _current?.TryGetTarget(out var activity) == true ? activity : null;

    protected override Widget CreateApp() => new MobileShell();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        _current = new WeakReference<MainActivity>(this);
        // До первого построения: MobileShell берёт платформенные сервисы при первом build.
        PlatformServices.Current = new AndroidPlatformServices();
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (Current == this)
            _current = null;
        base.OnDestroy();
    }
}
