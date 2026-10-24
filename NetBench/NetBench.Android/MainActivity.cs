using Android.Content.PM;
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
}
