using CommunityToolkit.Mvvm.ComponentModel;

namespace NetBench.Desktop.Services;

public interface INavigationService
{
    event Action<ObservableObject?>? PageChanged;

    /// <summary>Открытая сейчас страница.</summary>
    ObservableObject? Current { get; }

    void NavigateTo(ObservableObject? page);
}
