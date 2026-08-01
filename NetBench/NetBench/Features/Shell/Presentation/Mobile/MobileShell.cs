using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Scenarios.Presentation.Mobile;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Slang;
using Plumix.Widgets;

namespace NetBench.Features.Shell.Presentation.Mobile;

/// <summary>
/// Корневой виджет мобильного приложения: тема NetBench (дизайн-токены,
/// переключение dark/light), провайдеры domain/data-слоя и навигатор.
/// Мобильный composition root — аналог Pure.DI-композиции desktop-версии,
/// но в стиле Flutter (Repository/Bloc-провайдеры).
/// </summary>
public sealed class MobileShell : StatefulWidget
{
    public override State CreateState() => new MobileShellState();
}

internal sealed class MobileShellState : State
{
    private readonly JsonScenarioRepository _scenarios = new();
    private readonly InMemoryReportStore _reports = new();
    private bool _isDark = true;

    public override Widget Build(BuildContext context)
    {
        var palette = _isDark ? NetBenchPalette.Dark : NetBenchPalette.Light;

        // TranslationProvider — снаружи KeyedSubtree: он держит подписку на смену культуры,
        // и пересоздавать её на каждом переключении темы незачем. Смена локали перестраивает
        // всех, кто читал строки через Translations<Strings>.Of(context).
        return new TranslationProvider<Strings>(
            child: new KeyedSubtree(
            // KeyedSubtree: RenderParagraph в Plumix не перерисовывает текст при смене только цвета,
            // поэтому при переключении темы пересоздаём всё поддерево (тумблер доступен только на домашнем экране).
            key: new ValueKey<bool>(_isDark),
            child: new Theme(
            data: NetBenchTheme.CreateThemeData(palette),
            child: new NetBenchTheme(
                palette: palette,
                toggleTheme: () => SetState(() => _isDark = !_isDark),
                child: new ScaffoldMessenger(
                    child: new RepositoryProvider<IScenarioRepository>(
                        value: _scenarios,
                        child: new RepositoryProvider<IReportStore>(
                            value: _reports,
                            child: new BlocProvider<ScenarioListCubit>(
                                create: static providerContext =>
                                {
                                    var cubit = new ScenarioListCubit(
                                        RepositoryProvider<IScenarioRepository>.ReadOf(providerContext),
                                        RepositoryProvider<IReportStore>.ReadOf(providerContext));
                                    _ = cubit.LoadAsync();
                                    return cubit;
                                },
                                child: new Navigator(
                                    initialRoute: new BuilderPageRoute(
                                        static _ => new ScenarioListScreen(),
                                        settings: new RouteSettings("home")))))))))));
    }
}
