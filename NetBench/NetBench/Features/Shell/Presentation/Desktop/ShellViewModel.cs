using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Desktop.Services;

namespace NetBench.Features.Shell.Presentation.Desktop;

public partial class ShellViewModel : ObservableObject
{
    public ScenarioListViewModel Sidebar { get; }

    [ObservableProperty]
    private ObservableObject? _currentPage;

    public ShellViewModel(INavigationService navigation, ScenarioListViewModel sidebar)
    {
        Sidebar = sidebar;
        navigation.PageChanged += page => CurrentPage = page;
        _ = sidebar.LoadCommand.ExecuteAsync(null);
    }
}
