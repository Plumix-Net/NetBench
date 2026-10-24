using NetBench.Mobile.Platform;

namespace NetBench.iOS;

/// <summary>
/// iOS-реализация <see cref="IPlatformServices"/>: «Поделиться» — UIActivityViewController
/// поверх верхнего контроллера, экран — IdleTimerDisabled, ссылки — UIApplication.OpenUrl.
/// Вызывается с UI-потока Avalonia, он же главный поток UIKit.
/// </summary>
internal sealed class IosPlatformServices : IPlatformServices
{
    public Task ShareTextAsync(string text, string title) => PresentAsync(new NSString(text));

    public Task ShareFileAsync(string path, string mimeType, string title) => PresentAsync(NSUrl.FromFilename(path));

    public void SetKeepScreenOn(bool keepOn) => UIApplication.SharedApplication.IdleTimerDisabled = keepOn;

    public Task<bool> OpenUrlAsync(Uri uri) =>
        NSUrl.FromString(uri.AbsoluteUri) is { } url
            ? UIApplication.SharedApplication.OpenUrlAsync(url, new UIApplicationOpenUrlOptions())
            : Task.FromResult(false);

    private static Task PresentAsync(NSObject item)
    {
        var presenter = TopViewController()
            ?? throw new InvalidOperationException("No view controller to present the share sheet from");

        var sheet = new UIActivityViewController([item], null);
        // На iPhone меню снизу; popover-якорь нужен только iPad, но без него там падение.
        if (sheet.PopoverPresentationController is { } popover && presenter.View is { } view)
        {
            popover.SourceView = view;
            popover.SourceRect = view.Bounds;
        }

        return presenter.PresentViewControllerAsync(sheet, animated: true);
    }

    private static UIViewController? TopViewController()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(candidate => candidate.IsKeyWindow);

        var controller = window?.RootViewController;
        while (controller?.PresentedViewController is { } presented)
            controller = presented;
        return controller;
    }
}
