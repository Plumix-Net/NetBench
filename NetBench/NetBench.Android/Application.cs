using Android.Runtime;
using Avalonia;
using NetBench.Mobile;
using Avalonia.Android;

namespace NetBench.Android;

[Application]
public class Application : AvaloniaAndroidApplication<MobileApp>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
        .WithInterFont();
    }
}
