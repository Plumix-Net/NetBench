using NetBench.Core.Models;
using NetBench.Core.Storage;
using NetBench.Features.Report;
using NetBench.Features.Scenarios;
using NetBench.Features.Shell;
using NetBench.Features.TestRun;
using NetBench.Services;
using Pure.DI;
using static Pure.DI.Lifetime;

namespace NetBench.Composition;

internal partial class Composition
{
    static void Setup() => DI.Setup(nameof(Composition))
        .Hint(Hint.Resolve, "Off")

        // Singletons
        .Bind<INavigationService>().As(Singleton).To<NavigationService>()
        .Bind<ScenarioRepository>().As(Singleton).To<ScenarioRepository>()
        .Bind<ScenarioListViewModel>().As(Singleton).To<ScenarioListViewModel>()
        .Bind<ShellViewModel>().As(Singleton).To<ShellViewModel>()

        // Transient (created per navigation with runtime arg)
        .Bind<ScenarioEditorViewModel>().To<ScenarioEditorViewModel>()
        .Bind<TestRunViewModel>().To<TestRunViewModel>()
        .Bind<ReportViewModel>().To<ReportViewModel>()

        // Roots
        .Root<ShellViewModel>("Root")
        .Root<Func<LoadScenario, ScenarioEditorViewModel>>("CreateEditor")
        .Root<Func<LoadScenario, TestRunViewModel>>("CreateTestRun")
        .Root<Func<TestRunReport, ReportViewModel>>("CreateReport");
}
