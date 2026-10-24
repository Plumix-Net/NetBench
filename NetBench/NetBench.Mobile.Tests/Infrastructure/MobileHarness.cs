using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using NetBench.Features.Shell.Presentation.Mobile;
using NetBench.Localization;
using Plumix;
using Plumix.Rendering;
using Plumix.Slang;

namespace NetBench.Mobile.Tests.Infrastructure;

/// <summary>
/// <see cref="MobileShell"/> в headless-окне размера телефона: данные — во временном каталоге,
/// кадры качаются вручную, цели для тапов ищутся по дереву семантики, как их видит TalkBack.
/// </summary>
public sealed class MobileHarness : IDisposable
{
    private static readonly TimeSpan _frameStep = TimeSpan.FromMilliseconds(50);

    // Таймер кадров Plumix — DispatcherTimer, а в headless он не тикает: без ручной прокачки
    // UI рисуется один раз и больше ни на что не реагирует.
    private static readonly Action<TimeSpan?> _pumpFrame = typeof(SchedulerBinding)
        .GetMethod("PumpFrameForTests", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Action<TimeSpan?>>();

    private readonly Window _window;
    private readonly WidgetHost _host;
    private readonly SemanticsHandle _semantics;
    private TimeSpan _clock;

    public MobileHarness(AppSettings? settings = null, string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Directory.CreateTempSubdirectory("netbench-mobile-tests-").FullName;
        if (settings is not null)
            new JsonAppSettingsStore(SettingsPath).Save(settings);

        // Язык — глобальное состояние Slang: каждый тест начинает с базовой культуры
        LocaleSettings<Strings>.SetLocaleRaw("en-US");

        _semantics = SemanticsBinding.Instance.EnsureSemantics();
        _clock = SchedulerBinding.Instance.CurrentSystemFrameTimeStamp;
        _host = new WidgetHost { RootWidget = new MobileShell(DataDirectory) };
        _window = new Window { Width = 390, Height = 780, Content = _host };
        _window.Show();
        Pump();
    }

    public string DataDirectory { get; }

    public string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    public SemanticsNode Root => _host.SemanticsRoot ?? throw new InvalidOperationException("Semantics are off");

    /// <summary>Настройки так, как их прочтёт следующий запуск приложения.</summary>
    public AppSettings ReadSettings() => new JsonAppSettingsStore(SettingsPath).Current;

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

    /// <summary>Ждёт условия, качая кадры: для работы, которая идёт вне UI-потока (диск, движок).</summary>
    public async Task PumpUntilAsync(Func<bool> condition, string because, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out waiting until {because}. Visible: {string.Join(" | ", Labels()).ReplaceLineEndings("⏎")}");

            await Task.Delay(20);
            Pump(2);
        }
    }

    public IEnumerable<SemanticsNode> Nodes() => Descendants(Root);

    /// <summary>
    /// Узел с такой подписью или подсказкой (tooltip). Карточки сливают тексты потомков в одну
    /// многострочную подпись — совпадение с любой её строкой тоже считается.
    /// </summary>
    public SemanticsNode? FindByLabel(string label) =>
        Nodes().FirstOrDefault(node => node.Label == label || node.Tooltip == label)
        ?? Nodes().FirstOrDefault(node => node.Label.Split('\n').Contains(label));

    public SemanticsNode GetByLabel(string label) =>
        FindByLabel(label) ?? throw new InvalidOperationException(
            $"No semantics node labelled “{label}”. Visible: {string.Join(" | ", Labels()).ReplaceLineEndings("⏎")}");

    public bool HasLabel(string label) => FindByLabel(label) is not null;

    public IEnumerable<string> Labels() =>
        Nodes().Select(node => node.Label.Length > 0 ? node.Label : node.Tooltip).Where(label => label.Length > 0);

    public void Tap(string label) => Tap(GetByLabel(label));

    /// <summary>Тап мышью по центру узла — через hit-test хоста, как настоящий палец.</summary>
    public void Tap(SemanticsNode node)
    {
        var rect = node.GlobalRect;
        var point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        _window.MouseDown(point, MouseButton.Left);
        Pump(2);
        _window.MouseUp(point, MouseButton.Left);
        Pump();
    }

    /// <summary>Текст в поле с фокусом — как его вставляет клавиатура.</summary>
    public void Type(string text)
    {
        _window.KeyTextInput(text);
        Pump();
    }

    public void Dispose()
    {
        _host.RootWidget = null;
        Pump(2);
        _window.Close();
        _semantics.Dispose();
        LocaleSettings<Strings>.SetLocaleRaw("en-US");

        try
        {
            Directory.Delete(DataDirectory, recursive: true);
        }
        catch (IOException)
        {
            // отчёт мог дописываться движком в момент удаления — временный каталог не критичен
        }
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
