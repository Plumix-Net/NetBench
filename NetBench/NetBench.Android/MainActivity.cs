using Android.Content.PM;
using Plumix.Android;

namespace NetBench.Android;

// PlumixMainActivity передаёт фреймворку жизненный цикл, восстановление и системный «назад»
// (включая predictive back на Android 14+) — своих мостов в голове не нужно.
[Activity(
    Label = "NetBench.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : PlumixMainActivity
{
}
