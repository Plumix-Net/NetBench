using Android.Runtime;
using Avalonia;
using NetBench.Mobile;
using Avalonia.Android;
using NetBench.Mobile.Platform;

namespace NetBench.Android;

[Application]
public class Application : AvaloniaAndroidApplication<MobileApp>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        // До старта Avalonia: MobileShell берёт платформенные сервисы при первом построении.
        PlatformServices.Current = new AndroidPlatformServices();
        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
        .WithInterFont();
    }
}
