using Android.Content.PM;
using Android.OS;
using Plumix.Android;

namespace NetBench.Android;

// PlumixMainActivity передаёт фреймворку жизненный цикл, восстановление и системный «назад»
// (включая predictive back на Android 14+) — своих мостов в голове не нужно.
// Только портрет: вёрстка экранов рассчитана на телефон в вертикальной ориентации.
[Activity(
    Label = "NetBench",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@mipmap/ic_launcher",
    RoundIcon = "@mipmap/ic_launcher_round",
    MainLauncher = true,
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : PlumixMainActivity
{
    private static WeakReference<MainActivity>? _current;

    /// <summary>Текущая Activity — для Intent'ов и флагов окна из <see cref="AndroidPlatformServices"/>.</summary>
    internal static MainActivity? Current => _current?.TryGetTarget(out var activity) == true ? activity : null;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        _current = new WeakReference<MainActivity>(this);
        base.OnCreate(savedInstanceState);
    }

    protected override void OnDestroy()
    {
        if (Current == this)
            _current = null;
        base.OnDestroy();
    }
}
