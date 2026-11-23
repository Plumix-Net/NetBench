using System.Globalization;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;
using NetBench.Desktop;
using NetBench.Desktop.Composition;
using NetBench.Desktop.Views;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Data;
using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using NetBench.Features.Shell.Presentation.Desktop;
using NetBench.Localization;
using NetBench.Screenshots;
using SkiaSharp;

// Скриншоты Mac App Store (2880×1800) на каждом языке листинга. Запуск из корня репозитория:
//   dotnet run --project NetBench/NetBench.Screenshots.Desktop -c Release -- --output .
// Кадры: живой прогон → отчёт → сравнение двух сценариев → редактор сценария.

var root = Path.GetFullPath(OptionValue("--output") ?? ".");

using var target = new DemoTarget();
target.UseAsProxy();

AppBuilder.Configure<App>()
    .UseSkia()
    // Настоящая отрисовка Skia: без неё текст не измеряется, и раскладка не та, что в приложении
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .WithInterFont()
    .SetupWithoutStarting();

foreach (var listing in MacListing.All)
{
    var culture = CultureInfo.GetCultureInfo(listing.Culture);
    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
    CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;
    Strings.SetCulture(culture);

    var data = DemoData.For(listing.Culture);
    var captionSize = MacFrame.CaptionSize(listing.Captions);
    var shot = 0;

    void Save(DesktopSession session, string name)
    {
        using var window = session.Capture();
        using var frame = MacFrame.Marketing(window, listing.Captions[shot], captionSize);
        var file = $"{++shot:00}_{name}.png";
        MacFrame.SavePng(frame, Path.Combine(root, "fastlane", "screenshots", "macos", listing.AppStore, file));
        Console.WriteLine($"{listing.Culture}: {file}");
    }

    using (var session = new DesktopSession(data, dark: true))
    {
        var checkout = session.Scenario(data.Checkout.Name);
        session.Shell.Sidebar.RunScenarioCommand.Execute(checkout);
        session.Wait(TimeSpan.FromSeconds(11));
        Save(session, "monitor");

        session.Wait(data.Checkout.Load.Duration, until: () => session.Shell.CurrentPage is ReportViewModel);
        session.Wait(TimeSpan.FromSeconds(1));
        Save(session, "report");

        ((ReportViewModel)session.Shell.CurrentPage!).CompareCommand.Execute(null);
        session.Wait(TimeSpan.FromSeconds(1));
        Save(session, "compare");
    }

    using (var session = new DesktopSession(data, dark: false))
    {
        session.Shell.Sidebar.EditScenarioCommand.Execute(session.Scenario(data.Scenarios[1].Name));
        session.Wait(TimeSpan.FromSeconds(1));
        Save(session, "editor");
    }
}

return;

string? OptionValue(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

/// <summary>Язык листинга: культура приложения, код языка в App Store Connect, подписи кадров.</summary>
internal sealed record MacListing(string Culture, string AppStore, string[] Captions)
{
    public static readonly MacListing[] All =
    [
        new("en-US", "en-US",
        [
            "Watch RPS, p95 latency and errors live",
            "Percentiles, status codes and exports",
            "Compare scenarios side by side",
            "Scenarios with ramp-up, think time and steps",
        ]),
        new("ru-RU", "ru",
        [
            "RPS, p95 и ошибки в реальном времени",
            "Перцентили, коды ответов и экспорт",
            "Сравнение сценариев бок о\u00A0бок",
            "Сценарии с разгоном, паузами и шагами",
        ]),
    ];
}

/// <summary>
/// Desktop-окно NetBench на демо-данных: сценарии и настройки — во временном каталоге
/// и история прогонов — во временном каталоге (<see cref="DesktopStorage"/>). Данные машины не трогаются.
/// </summary>
internal sealed class DesktopSession : IDisposable
{
    public const double Width = 1100; // MinWidth окна — крупнее текст в кадре
    public const double Height = 688;
    public const double Scale = 2;

    private readonly string _directory;
    private readonly MainWindow _window;

    public DesktopSession(DemoData data, bool dark)
    {
        _directory = Directory.CreateTempSubdirectory("netbench-desktop-screenshots-").FullName;
        var storage = new DesktopStorage(_directory);

        var repository = new JsonScenarioRepository(storage.ScenariosDirectory);
        foreach (var scenario in data.Scenarios)
            repository.SaveAsync(scenario).GetAwaiter().GetResult();
        new JsonAppSettingsStore(storage.SettingsPath).Save(new AppSettings { OwnTargetsConfirmed = true });

        Application.Current!.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        var composition = new Composition(storage);
        foreach (var report in data.History.Reverse())
            composition.Reports.Save(report);

        Shell = composition.Root;
        _window = new MainWindow { DataContext = Shell, Width = Width, Height = Height };
        _window.Show();
        _window.SetRenderScaling(Scale);
        Wait(TimeSpan.FromSeconds(1), until: () => Shell.Sidebar.Scenarios.Count == data.Scenarios.Count);
    }

    public ShellViewModel Shell { get; }

    public NetBench.Features.Scenarios.Presentation.Desktop.ScenarioViewModel Scenario(string name) =>
        Shell.Sidebar.Scenarios.First(s => s.Model.Name == name);

    /// <summary>Крутит диспетчер и кадры: прогон идёт в реальном времени на пуле потоков.</summary>
    public void Wait(TimeSpan duration, Func<bool>? until = null)
    {
        var end = DateTime.UtcNow + duration + (until is null ? TimeSpan.Zero : TimeSpan.FromSeconds(15));
        var minimum = DateTime.UtcNow + (until is null ? duration : TimeSpan.Zero);
        while (DateTime.UtcNow < end)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            if (DateTime.UtcNow >= minimum && (until is null || until()))
                return;
            Thread.Sleep(16);
        }

        if (until is not null)
            throw new TimeoutException("Screen state not reached");
    }

    public SKBitmap Capture()
    {
        Wait(TimeSpan.FromMilliseconds(300));
        var frame = _window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame rendered");
        using var png = new MemoryStream();
        frame.Save(png);
        png.Position = 0;
        return SKBitmap.Decode(png);
    }

    public void Dispose()
    {
        _window.Close();
        Dispatcher.UIThread.RunJobs();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
