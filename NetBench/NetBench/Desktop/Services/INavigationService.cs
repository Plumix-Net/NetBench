using CommunityToolkit.Mvvm.ComponentModel;

namespace NetBench.Desktop.Services;

public interface INavigationService
{
    event Action<ObservableObject?>? PageChanged;
    void NavigateTo(ObservableObject? page);
}
