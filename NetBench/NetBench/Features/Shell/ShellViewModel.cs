using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Scenarios;
using NetBench.Services;

namespace NetBench.Features.Shell;

public partial class ShellViewModel : ObservableObject
{
    public ScenarioListViewModel Sidebar { get; }

    [ObservableProperty]
    private ObservableObject? _currentPage;

    public ShellViewModel(INavigationService navigation, ScenarioListViewModel sidebar)
    {
        Sidebar = sidebar;
        navigation.PageChanged += page => CurrentPage = page;
    }
}
