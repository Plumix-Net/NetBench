using NetBench.Features.Shell.Presentation.Mobile;
using Plumix.iOS;
using Plumix.Widgets;

namespace NetBench.iOS;

// Голова iOS на нативном хосте Plumix без Avalonia (Plumix.iOS, рендер Impeller на Metal): PlumixViewController
// передаёт фреймворку жизненный цикл, настройки, ввод, клавиатуру, доступность и восстановление состояния — своих
// мостов в голове не нужно. Одна сцена; её корень — NetBench-контроллер.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Имя по соглашению UIKit.
public sealed class AppDelegate : UIApplicationDelegate
#pragma warning restore CA1711
{
    public override UISceneConfiguration GetConfiguration(
        UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options) =>
        new(null, connectingSceneSession.Role) { DelegateType = typeof(SceneDelegate) };
}

/// <summary>Сцена NetBench: окно с <see cref="NetBenchViewController"/>; состояние сохраняется вместе со сценой.</summary>
[Register("SceneDelegate")]
public sealed class SceneDelegate : PlumixSceneDelegate
{
    protected override PlumixViewController CreateViewController() => new NetBenchViewController();
}

/// <summary>Корневой контроллер: мобильный UI NetBench (<see cref="MobileShell"/>).</summary>
public sealed class NetBenchViewController : PlumixViewController
{
    public NetBenchViewController()
    {
        // Идентификатор восстановления включает восстановление состояния фреймворка (как у FlutterViewController).
        RestorationIdentifier = "netbench";
    }

    protected override Widget CreateApp() => new MobileShell();
}
