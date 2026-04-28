using CommunityToolkit.Mvvm.ComponentModel;

namespace NetBench.Services;

public interface INavigationService
{
    event Action<ObservableObject?>? PageChanged;
    void NavigateTo(ObservableObject? page);
}
