using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Core.Models;
using NetBench.Core.Storage;
using NetBench.Features.TestRun;
using NetBench.Services;

namespace NetBench.Features.Scenarios;

public partial class ScenarioListViewModel : ObservableObject
{
    private readonly ScenarioRepository _repository;
    private readonly INavigationService _navigation;
    private readonly Func<LoadScenario, ScenarioEditorViewModel> _createEditor;
    private readonly Func<LoadScenario, TestRunViewModel> _createTestRun;

    [ObservableProperty]
    private ObservableCollection<LoadScenario> _scenarios = [];

    [ObservableProperty]
    private LoadScenario? _selectedScenario;

    public ScenarioListViewModel(
        ScenarioRepository repository,
        INavigationService navigation,
        Func<LoadScenario, ScenarioEditorViewModel> createEditor,
        Func<LoadScenario, TestRunViewModel> createTestRun)
    {
        _repository = repository;
        _navigation = navigation;
        _createEditor = createEditor;
        _createTestRun = createTestRun;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        var all = await _repository.LoadAllAsync(ct);
        Scenarios = new ObservableCollection<LoadScenario>(all);
    }

    [RelayCommand]
    private async Task AddScenarioAsync(CancellationToken ct)
    {
        var scenario = new LoadScenario { Name = "New scenario" };
        scenario.Requests.Add(new RequestStep { Method = "GET", Path = "/" });
        await _repository.SaveAsync(scenario, ct);
        Scenarios.Add(scenario);
        _navigation.NavigateTo(_createEditor(scenario));
    }

    [RelayCommand]
    private void EditScenario(LoadScenario scenario) =>
        _navigation.NavigateTo(_createEditor(scenario));

    [RelayCommand]
    private void RunScenario(LoadScenario scenario) =>
        _navigation.NavigateTo(_createTestRun(scenario));

    [RelayCommand]
    private async Task DeleteScenarioAsync(LoadScenario scenario, CancellationToken ct)
    {
        await _repository.DeleteAsync(scenario.Id, ct);
        Scenarios.Remove(scenario);

        if (CurrentPage(scenario))
            _navigation.NavigateTo(null);
    }

    private bool CurrentPage(LoadScenario scenario) =>
        SelectedScenario?.Id == scenario.Id;
}
