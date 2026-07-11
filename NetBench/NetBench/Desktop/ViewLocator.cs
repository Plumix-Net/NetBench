using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Features.Shell.Presentation.Desktop;
using NetBench.Features.TestRun.Presentation.Desktop;

namespace NetBench.Desktop;

public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Registry = new()
    {
        [typeof(ShellViewModel)]          = static () => new ShellView(),
        [typeof(ScenarioListViewModel)]   = static () => new ScenarioListView(),
        [typeof(ScenarioEditorViewModel)] = static () => new ScenarioEditorView(),
        [typeof(TestRunViewModel)]        = static () => new TestRunView(),
        [typeof(ReportViewModel)]         = static () => new ReportView(),
    };

    public Control? Build(object? param) =>
        param is not null && Registry.TryGetValue(param.GetType(), out var factory)
            ? factory()
            : new TextBlock { Text = $"No view for: {param?.GetType().Name}" };

    public bool Match(object? data) => data is ObservableObject;
}
