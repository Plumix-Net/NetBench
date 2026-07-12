using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Features.Shell.Presentation.Desktop;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Localization;

namespace NetBench.Desktop;

public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> _registry = new()
    {
        [typeof(ShellViewModel)]          = static () => new ShellView(),
        [typeof(ScenarioListViewModel)]   = static () => new ScenarioListView(),
        [typeof(ScenarioEditorViewModel)] = static () => new ScenarioEditorView(),
        [typeof(TestRunViewModel)]        = static () => new TestRunView(),
        [typeof(ReportViewModel)]         = static () => new ReportView(),
        [typeof(CompareViewModel)]        = static () => new CompareView(),
    };

    public Control? Build(object? param) =>
        param is not null && _registry.TryGetValue(param.GetType(), out var factory)
            ? factory()
            : new TextBlock
            {
                Text = Strings.Instance.Root.Diagnostics.ViewNotFound(
                    param?.GetType().Name ?? string.Empty),
            };

    public bool Match(object? data) => data is ObservableObject;
}
