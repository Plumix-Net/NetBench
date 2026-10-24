using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using NetBench.Features.Settings.Domain;
using NetBench.Localization;
using NetBench.Screenshots;
using SkiaSharp;

// Скриншоты для App Store (iPhone 6.9", 1320×2868) и Google Play (телефон, 1080×1920) на каждом языке
// листинга. Запуск из корня репозитория: bundle exec fastlane screenshots, или напрямую
//   dotnet run --project NetBench/NetBench.Screenshots -c Release -- --output .
// Ключи: --output <корень репозитория> (по умолчанию текущий каталог), --dump — печатать дерево
// семантики каждого кадра (подписи для тапов).

var root = Path.GetFullPath(OptionValue("--output") ?? ".");
var dump = args.Contains("--dump");

// Цель живого прогона — DemoTarget за http_proxy; переменная читается при первом HttpClient.
using var target = new DemoTarget();
target.UseAsProxy();

AppBuilder.Configure<ShotApp>()
    .UseSkia()
    // Настоящая отрисовка Skia: без неё текст не измеряется, и раскладка экранов не та, что на устройстве
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont()
    .SetupWithoutStarting();

foreach (var listing in Listing.All)
{
    var data = DemoData.For(listing.Culture);
    var shot = 0;

    void Save(ShotSession session, string name)
    {
        if (dump)
            Console.WriteLine($"[{listing.Culture} {name}] " + string.Join(" | ", session.Labels()));

        using var app = session.Capture();
        using var phone = StoreFrame.Phone(app, session.IsDark);
        var caption = listing.Captions[shot];
        var file = $"{++shot:00}_{name}.png";

        using (var ios = StoreFrame.Marketing(phone, caption, StoreFrame.CaptionSize(listing.Captions, 1320), 1320, 2868))
            StoreFrame.SavePng(ios, Path.Combine(root, "fastlane", "screenshots", "ios", listing.AppStore, file));
        using (var play = StoreFrame.Marketing(phone, caption, StoreFrame.CaptionSize(listing.Captions, 1080), 1080, 1920))
            StoreFrame.SavePng(play, Path.Combine(root, "fastlane", "metadata", "android", listing.GooglePlay, "images", "phoneScreenshots", file));
        Console.WriteLine($"{listing.Culture}: {file}");
    }

    AppSettings Settings(ThemePreference theme) => new()
    {
        Theme = theme,
        Culture = listing.Culture,
        OwnTargetsConfirmed = true,
    };

    using (var session = new ShotSession(Settings(ThemePreference.Light), data))
        Save(session, "home");

    using (var session = new ShotSession(Settings(ThemePreference.Light), data))
    {
        var strings = Strings.Instance.Root;
        session.Tap(strings.Mobile.QuickTest);
        session.Tap(session.Get(strings.Scenarios.Editor.TargetPlaceholder));
        session.Type($"{DemoData.LanHost}/api/products");
        Save(session, "quick_test");
    }

    using (var session = new ShotSession(Settings(ThemePreference.Dark), data))
    {
        var strings = Strings.Instance.Root;
        session.Tap(data.Checkout.Name);
        session.Wait(TimeSpan.FromSeconds(11));
        Save(session, "monitor");

        session.Wait(data.Checkout.Load.Duration, until: () => session.Has(strings.Mobile.ResultTitle));
        session.Wait(TimeSpan.FromSeconds(1));
        Save(session, "result");
    }

    using (var session = new ShotSession(Settings(ThemePreference.Light), data))
    {
        session.Tap(Strings.Instance.Root.Scenarios.Edit);
        Save(session, "editor");
    }
}

return;

string? OptionValue(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

/// <summary>
/// Язык листинга: культура приложения, коды языка в App Store Connect и Play Console, подписи кадров
/// (неразрывный пробел \u00A0 не даёт предлогу остаться в конце строки).
/// </summary>
internal sealed record Listing(string Culture, string AppStore, string GooglePlay, string[] Captions)
{
    public static readonly Listing[] All =
    [
        new("en-US", "en-US", "en-US",
        [
            "Load test your API right from your phone",
            "Enter a URL, pick users and duration",
            "Watch RPS, p95 latency and errors live",
            "Percentiles, status codes and a\u00A0shareable report",
            "Save scenarios for the endpoints you test",
        ]),
        new("ru-RU", "ru", "ru-RU",
        [
            "Нагрузочный тест API прямо с\u00A0телефона",
            "Адрес, пользователи, длительность — и вперёд",
            "RPS, p95 и\u00A0ошибки в\u00A0реальном времени",
            "Перцентили, коды ответов и\u00A0отчёт для\u00A0отправки",
            "Сценарии для\u00A0ваших эндпоинтов",
        ]),
    ];
}

internal sealed class ShotApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}
