using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Desktop.Services;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public partial class ScenarioListViewModel : ObservableObject
{
    private readonly IScenarioRepository _repository;
    private readonly INavigationService _navigation;
    private readonly Func<ScenarioViewModel, ScenarioEditorViewModel> _createEditor;
    private readonly Func<LoadScenario, TestRunViewModel> _createTestRun;

    [ObservableProperty]
    private ObservableCollection<ScenarioViewModel> _scenarios = [];

    [ObservableProperty]
    private ScenarioViewModel? _selectedScenario;

    public ScenarioListViewModel(
        IScenarioRepository repository,
        INavigationService navigation,
        Func<ScenarioViewModel, ScenarioEditorViewModel> createEditor,
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
        Scenarios = new ObservableCollection<ScenarioViewModel>(all.Select(s => new ScenarioViewModel(s)));
    }

    [RelayCommand]
    private async Task AddScenarioAsync(CancellationToken ct)
    {
        var scenario = new LoadScenario { Name = "New scenario" };
        scenario.Requests.Add(new RequestStep { Method = "GET", Path = "/" });
        await _repository.SaveAsync(scenario, ct);

        var vm = new ScenarioViewModel(scenario);
        Scenarios.Add(vm);
        _navigation.NavigateTo(_createEditor(vm));
    }

    [RelayCommand]
    private void EditScenario(ScenarioViewModel scenario) =>
        _navigation.NavigateTo(_createEditor(scenario));

    [RelayCommand]
    private void RunScenario(ScenarioViewModel scenario) =>
        _navigation.NavigateTo(_createTestRun(scenario.Model));

    [RelayCommand]
    private async Task DeleteScenarioAsync(ScenarioViewModel scenario, CancellationToken ct)
    {
        await _repository.DeleteAsync(scenario.Model.Id, ct);
        Scenarios.Remove(scenario);

        if (CurrentPage(scenario))
            _navigation.NavigateTo(null);
    }

    private bool CurrentPage(ScenarioViewModel scenario) =>
        SelectedScenario?.Model.Id == scenario.Model.Id;
}
