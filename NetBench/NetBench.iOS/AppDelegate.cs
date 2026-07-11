using Avalonia;
using NetBench.Mobile;
using Avalonia.iOS;

namespace NetBench.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
// Мобильная голова бутстрапит Plumix-версию UI (MobileApp), а не desktop-App.
public partial class AppDelegate : AvaloniaAppDelegate<MobileApp>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
