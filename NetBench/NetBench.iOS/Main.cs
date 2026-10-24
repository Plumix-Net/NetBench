using NetBench.Mobile.Platform;

namespace NetBench.iOS;

public class Application
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // До старта Avalonia: MobileShell берёт платформенные сервисы при первом построении.
        PlatformServices.Current = new IosPlatformServices();

        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
