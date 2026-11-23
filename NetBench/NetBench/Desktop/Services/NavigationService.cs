using CommunityToolkit.Mvvm.ComponentModel;

namespace NetBench.Desktop.Services;

internal sealed class NavigationService : INavigationService
{
    public event Action<ObservableObject?>? PageChanged;

    public ObservableObject? Current { get; private set; }

    public void NavigateTo(ObservableObject? page)
    {
        Current = page;
        PageChanged?.Invoke(page);
    }
}
