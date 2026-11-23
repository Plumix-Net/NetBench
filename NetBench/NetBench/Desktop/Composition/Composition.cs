using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.Scenarios.Data;
using NetBench.Features.TestRun.Data;
using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Features.Shell.Presentation.Desktop;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Desktop.Services;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace NetBench.Desktop.Composition;

internal partial class Composition
{
    static void Setup() => DI.Setup(nameof(Composition))
        .Hint(Hint.Resolve, "Off")
        .Arg<DesktopStorage>("storage")

        // Singletons
        .Bind<INavigationService>().As(Singleton).To<NavigationService>()
        .Bind<IScenarioRepository>().As(Singleton).To((DesktopStorage storage) => new JsonScenarioRepository(storage.ScenariosDirectory))
        .Bind<IReportStore>().As(Singleton).To((DesktopStorage storage) => new JsonReportStore(storage.HistoryPath))
        .Bind<IThemeService>().As(Singleton).To<ThemeService>()
        .Bind<IFileSaveService>().As(Singleton).To<FileSaveService>()
        .Bind<IAppSettingsStore>().As(Singleton).To((DesktopStorage storage) => new JsonAppSettingsStore(storage.SettingsPath))
        .Bind<IConfirmationDialog>().As(Singleton).To<ConfirmationDialog>()
        .Bind<RunConsent>().As(Singleton).To<RunConsent>()
        .Bind<ScenarioListViewModel>().As(Singleton).To<ScenarioListViewModel>()
        .Bind<ShellViewModel>().As(Singleton).To<ShellViewModel>()

        // Transient (created per navigation with runtime arg)
        .Bind<ScenarioEditorViewModel>().To<ScenarioEditorViewModel>()
        .Bind<TestRunViewModel>().To<TestRunViewModel>()
        .Bind<ReportViewModel>().To<ReportViewModel>()
        .Bind<CompareViewModel>().To<CompareViewModel>()

        // Roots
        .Root<ShellViewModel>("Root")
        // Генератор скриншотов заполняет историю прогонов до показа окна
        .Root<IReportStore>("Reports")
        .Root<Func<ScenarioViewModel, ScenarioEditorViewModel>>("CreateEditor")
        .Root<Func<LoadScenario, TestRunViewModel>>("CreateTestRun")
        .Root<Func<TestRunReport, ReportViewModel>>("CreateReport")
        .Root<Func<TestRunReport, CompareViewModel>>("CreateCompare");
}
