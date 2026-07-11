using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.Scenarios.Data;
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

        // Singletons
        .Bind<INavigationService>().As(Singleton).To<NavigationService>()
        .Bind<IScenarioRepository>().As(Singleton).To<JsonScenarioRepository>()
        .Bind<ScenarioListViewModel>().As(Singleton).To<ScenarioListViewModel>()
        .Bind<ShellViewModel>().As(Singleton).To<ShellViewModel>()

        // Transient (created per navigation with runtime arg)
        .Bind<ScenarioEditorViewModel>().To<ScenarioEditorViewModel>()
        .Bind<TestRunViewModel>().To<TestRunViewModel>()
        .Bind<ReportViewModel>().To<ReportViewModel>()

        // Roots
        .Root<ShellViewModel>("Root")
        .Root<Func<ScenarioViewModel, ScenarioEditorViewModel>>("CreateEditor")
        .Root<Func<LoadScenario, TestRunViewModel>>("CreateTestRun")
        .Root<Func<TestRunReport, ReportViewModel>>("CreateReport");
}
