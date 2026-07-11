using CommunityToolkit.Mvvm.ComponentModel;

namespace NetBench.Desktop.Services;

internal sealed class NavigationService : INavigationService
{
    public event Action<ObservableObject?>? PageChanged;

    public void NavigateTo(ObservableObject? page) => PageChanged?.Invoke(page);
}
