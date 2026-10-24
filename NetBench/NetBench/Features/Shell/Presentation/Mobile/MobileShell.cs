using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Scenarios.Presentation.Mobile;
using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using NetBench.Localization;
using NetBench.Mobile.Theme;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Slang;
using Plumix.UI;
using Plumix.Widgets;
using Colors = Plumix.Material.Colors;

namespace NetBench.Features.Shell.Presentation.Mobile;

/// <summary>
/// Корневой виджет мобильного приложения: тема NetBench (дизайн-токены,
/// dark/light/как в системе), провайдеры domain/data-слоя и навигатор.
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
    private readonly JsonReportStore _reports = new();
    private readonly JsonAppSettingsStore _settings = new();

    public override void InitState()
    {
        base.InitState();

        // Язык восстанавливаем до первого Build — иначе первый кадр мелькнёт на языке устройства.
        if (_settings.Current.Culture is { } culture)
            LocaleSettings<Strings>.SetLocaleRaw(culture);
    }

    private void SetThemePreference(ThemePreference preference)
    {
        if (preference == _settings.Current.Theme)
            return;

        SetState(() => _settings.Save(_settings.Current with { Theme = preference }));
    }

    public override Widget Build(BuildContext context)
    {
        var preference = _settings.Current.Theme;
        var isDark = preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            // MediaQuery от View: при смене темы устройства Build вызовется снова
            _ => MediaQuery.PlatformBrightnessOf(context) == PlatformBrightness.Dark,
        };
        var palette = isDark ? NetBenchPalette.Dark : NetBenchPalette.Light;

        // TranslationProvider держит подписку на смену культуры и перестраивает всех,
        // кто читал строки через Translations<Strings>.Of(context).
        // Провайдеры стоят над MaterialApp, чтобы их видели все роуты его навигатора.
        // MaterialApp, а не голый Navigator: системный «назад» приходит в WidgetsBinding.HandlePopRoute,
        // и до навигатора его доводит именно WidgetsApp (он же сообщает Android, есть ли куда уходить).
        return new TranslationProvider<Strings>(
            child: new NetBenchTheme(
                palette: palette,
                preference: preference,
                setPreference: SetThemePreference,
                child: new RepositoryProvider<IScenarioRepository>(
                    value: _scenarios,
                    child: new RepositoryProvider<IReportStore>(
                        value: _reports,
                        child: new RepositoryProvider<IAppSettingsStore>(
                            value: _settings,
                            child: new BlocProvider<ScenarioListCubit>(
                                create: static providerContext =>
                                {
                                    var cubit = new ScenarioListCubit(
                                        RepositoryProvider<IScenarioRepository>.ReadOf(providerContext),
                                        RepositoryProvider<IReportStore>.ReadOf(providerContext));
                                    _ = cubit.LoadAsync();
                                    return cubit;
                                },
                                child: new AnnotatedRegion<SystemUiOverlayStyle>(
                                    value: SystemBarsStyle(isDark),
                                    child: new MaterialApp(
                                        onGenerateTitle: static titleContext =>
                                            Translations<Strings>.Of(titleContext).App.Title,
                                        theme: NetBenchTheme.CreateThemeData(palette),
                                        debugShowCheckedModeBanner: false,
                                        home: new ScenarioListScreen()))))))));
    }

    /// <summary>
    /// Прозрачные системные панели — хост рисует edge-to-edge (без стиля iOS оставлял под
    /// статус-баром и home-индикатором белые полосы), иконки — контрастом к фону темы.
    /// Отступы под панели экраны берут сами через SafeArea.
    /// </summary>
    private static SystemUiOverlayStyle SystemBarsStyle(bool isDark) => new(
        statusBarColor: Colors.Transparent,
        systemNavigationBarColor: Colors.Transparent,
        statusBarIconBrightness: isDark ? PlatformBrightness.Light : PlatformBrightness.Dark,
        systemNavigationBarIconBrightness: isDark ? PlatformBrightness.Light : PlatformBrightness.Dark,
        statusBarBrightness: isDark ? PlatformBrightness.Dark : PlatformBrightness.Light);
}
