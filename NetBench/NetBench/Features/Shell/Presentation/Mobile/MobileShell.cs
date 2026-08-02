using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Scenarios.Presentation.Mobile;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using NetBench.Mobile.Navigation;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
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

        // TranslationProvider держит подписку на смену культуры и перестраивает всех,
        // кто читал строки через Translations<Strings>.Of(context).
        // Пересоздание поддерева при смене темы (Plumix не перекрашивает уже размеченный
        // текст) живёт не здесь, а внутри каждого роута — см. ThemedPageRoute:
        // так стек навигации переживает переключение и тумблер доступен с любого экрана.
        return new TranslationProvider<Strings>(
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
                                    initialRoute: ThemedPageRoute.Of(
                                        static _ => new ScenarioListScreen(),
                                        new RouteSettings("home"))))))))));
    }
}
