using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using NetBench.Features.Scenarios.Data;
using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using NetBench.Features.Shell.Presentation.Mobile;
using NetBench.Features.TestRun.Data;
using NetBench.Localization;
using Plumix;
using Plumix.Rendering;
using Plumix.Slang;
using SkiaSharp;

namespace NetBench.Screenshots;

/// <summary>
/// Один запуск приложения для съёмки: <see cref="MobileShell"/> в headless-окне размера iPhone 6.9"
/// (440×<see cref="ScreenHeight"/> pt, масштаб 3 — ровно пиксели скриншота App Store без полос
/// статус-бара и home-индикатора, их дорисовывает <see cref="StoreFrame"/>). Данные — демо, во временном
/// каталоге (данные приложения на машине не трогаются). Кадры качаются вручную, цели для тапов ищутся
/// по дереву семантики, как их видит VoiceOver/TalkBack.
/// </summary>
internal sealed class ShotSession : IDisposable
{
    public const double ScreenWidth = 440;
    public const double StatusBarHeight = 62;
    public const double HomeIndicatorHeight = 34;
    public const double ScreenHeight = 956 - StatusBarHeight - HomeIndicatorHeight;
    public const double Scale = 3;

    private static readonly TimeSpan _frameStep = TimeSpan.FromMilliseconds(50);

    // Таймер кадров Plumix — DispatcherTimer, а в headless он не тикает: без ручной прокачки
    // UI рисуется один раз и больше ни на что не реагирует.
    private static readonly Action<TimeSpan?> _pumpFrame = typeof(SchedulerBinding)
        .GetMethod("PumpFrameForTests", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Action<TimeSpan?>>();

    private readonly Window _window;
    private readonly WidgetHost _host;
    private readonly SemanticsHandle _semantics;
    private readonly string _dataDirectory;
    private TimeSpan _clock;

    public ShotSession(AppSettings settings, DemoData data)
    {
        IsDark = settings.Theme == ThemePreference.Dark;
        _dataDirectory = Directory.CreateTempSubdirectory("netbench-screenshots-").FullName;
        Seed(_dataDirectory, settings, data);
        LocaleSettings<Strings>.SetLocaleRaw(settings.Culture ?? "en-US");

        _semantics = SemanticsBinding.Instance.EnsureSemantics();
        _clock = SchedulerBinding.Instance.CurrentSystemFrameTimeStamp;
        // FocusAdorner = null: иначе Fluent рисует рамку фокуса по краю хоста прямо в кадре
        _host = new WidgetHost { RootWidget = new MobileShell(_dataDirectory), FocusAdorner = null };
        _window = new Window { Width = ScreenWidth, Height = ScreenHeight, Content = _host };
        _window.Show();
        _window.SetRenderScaling(Scale);
        Pump(40);
    }

    public bool IsDark { get; }

    public SemanticsNode Root => _host.SemanticsRoot ?? throw new InvalidOperationException("Semantics are off");

    /// <summary>Прокачивает очередь диспетчера и кадры с синтетическим временем (анимации переходов доигрывают).</summary>
    public void Pump(int frames = 20)
    {
        for (var i = 0; i < frames; i++)
        {
            Dispatcher.UIThread.RunJobs();
            _clock += _frameStep;
            _pumpFrame(_clock);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Живёт в реальном времени: для прогона, который идёт вне UI-потока.</summary>
    public void Wait(TimeSpan duration, Func<bool>? until = null)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration && until?.Invoke() != true)
        {
            Thread.Sleep(25);
            Pump(1);
        }

        Pump();
    }

    public IEnumerable<SemanticsNode> Nodes() => Descendants(Root);

    public IEnumerable<string> Labels() =>
        Nodes().Select(node => node.Label.Length > 0 ? node.Label : node.Tooltip).Where(label => label.Length > 0);

    public bool Has(string label) => Find(label) is not null;

    /// <summary>Узел с точной подписью, а если такого нет — первый, чья подпись начинается с неё.</summary>
    public SemanticsNode? Find(string label) =>
        Nodes().FirstOrDefault(node => node.Label == label || node.Tooltip == label)
        ?? Nodes().FirstOrDefault(node => node.Label.StartsWith(label, StringComparison.Ordinal));

    public SemanticsNode Get(string label) =>
        Find(label) ?? throw new InvalidOperationException(
            $"No semantics node labelled “{label}”. Visible: {string.Join(" | ", Labels())}");

    public void Tap(string label) => Tap(Get(label));

    /// <summary>Тап мышью по центру узла — через hit-test хоста, как настоящий палец.</summary>
    public void Tap(SemanticsNode node)
    {
        var rect = node.GlobalRect;
        var point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _window.MouseDown(point, MouseButton.Left);
        Pump(2);
        _window.MouseUp(point, MouseButton.Left);
        Pump(30);
    }

    /// <summary>Текст в поле с фокусом — как его вставляет клавиатура.</summary>
    public void Type(string text)
    {
        _window.KeyTextInput(text);
        Pump();
    }

    /// <summary>Кадр в пикселях устройства (1320×2682).</summary>
    public SKBitmap Capture()
    {
        Pump(4);
        var frame = _window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame rendered");
        using var png = new MemoryStream();
        frame.Save(png);
        png.Position = 0;
        return SKBitmap.Decode(png);
    }

    public void Dispose()
    {
        _host.RootWidget = null;
        Pump(2);
        _window.Close();
        _semantics.Dispose();

        try
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
        catch (IOException)
        {
            // временный каталог не критичен
        }
    }

    /// <summary>Раскладывает демо-данные так же, как их сохранило бы приложение (раскладка — MobileShell).</summary>
    private static void Seed(string directory, AppSettings settings, DemoData data)
    {
        new JsonAppSettingsStore(Path.Combine(directory, "settings.json")).Save(settings);

        // Вне UI-потока: SaveAsync возвращается в контекст синхронизации (await using), а его поток ждёт здесь.
        var scenarios = new JsonScenarioRepository(Path.Combine(directory, "scenarios"));
        Task.Run(async () =>
        {
            foreach (var scenario in data.Scenarios)
                await scenarios.SaveAsync(scenario);
        }).GetAwaiter().GetResult();

        // История — новые первыми, а Save кладёт в начало: сохраняем с конца.
        var reports = new JsonReportStore(Path.Combine(directory, "history.json"));
        for (var i = data.History.Count - 1; i >= 0; i--)
            reports.Save(data.History[i]);
    }

    private static IEnumerable<SemanticsNode> Descendants(SemanticsNode node)
    {
        var children = new List<SemanticsNode>();
        node.VisitChildren(child =>
        {
            children.Add(child);
            return true;
        });

        foreach (var child in children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
