using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Desktop.Services;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public partial class ScenarioListViewModel : ObservableObject
{
    private readonly IScenarioRepository _repository;
    private readonly INavigationService _navigation;
    private readonly IReportStore _reports;
    private readonly IThemeService _theme;
    private readonly Func<ScenarioViewModel, ScenarioEditorViewModel> _createEditor;
    private readonly Func<LoadScenario, TestRunViewModel> _createTestRun;
    private readonly Func<TestRunReport, ReportViewModel> _createReport;

    [ObservableProperty]
    private ObservableCollection<ScenarioViewModel> _scenarios = [];

    [ObservableProperty]
    private ScenarioViewModel? _selectedScenario;

    public ScenarioListViewModel(
        IScenarioRepository repository,
        INavigationService navigation,
        IReportStore reports,
        IThemeService theme,
        Func<ScenarioViewModel, ScenarioEditorViewModel> createEditor,
        Func<LoadScenario, TestRunViewModel> createTestRun,
        Func<TestRunReport, ReportViewModel> createReport)
    {
        _repository = repository;
        _navigation = navigation;
        _reports = reports;
        _theme = theme;
        _createEditor = createEditor;
        _createTestRun = createTestRun;
        _createReport = createReport;
        _theme.Changed += () => OnPropertyChanged(nameof(IsDark));
    }

    public bool IsDark => _theme.IsDark;

    [RelayCommand]
    private void ToggleTheme() => _theme.Toggle();

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        var all = await _repository.LoadAllAsync(ct);
        Scenarios = new ObservableCollection<ScenarioViewModel>(all.Select(s => new ScenarioViewModel(s)));
        SelectedScenario = Scenarios.FirstOrDefault();
    }

    /// <summary>Клик по строке: отчёт последнего прогона, а если его нет — редактор.</summary>
    partial void OnSelectedScenarioChanged(ScenarioViewModel? value)
    {
        if (value is null)
        {
            _navigation.NavigateTo(null);
            return;
        }

        var report = _reports.GetLatest(value.Model.Id);
        _navigation.NavigateTo(report is not null
            ? _createReport(report)
            : _createEditor(value));
    }

    [RelayCommand]
    private async Task AddScenarioAsync(CancellationToken ct)
    {
        var scenario = new LoadScenario { Name = "Новый сценарий" };
        await _repository.SaveAsync(scenario, ct);

        var vm = new ScenarioViewModel(scenario);
        Scenarios.Add(vm);
        SelectedScenario = vm; // выбор сам откроет редактор — отчёта ещё нет
    }

    [RelayCommand]
    private void EditScenario(ScenarioViewModel scenario)
    {
        SelectedScenario = scenario;
        _navigation.NavigateTo(_createEditor(scenario));
    }

    [RelayCommand]
    private void RunScenario(ScenarioViewModel scenario)
    {
        SelectedScenario = scenario;

        // Без цели запускать нечего — редактор покажет ошибку валидации.
        if (!scenario.HasTarget)
        {
            _navigation.NavigateTo(_createEditor(scenario));
            return;
        }

        _navigation.NavigateTo(_createTestRun(scenario.Model));
    }

    [RelayCommand]
    private async Task DeleteScenarioAsync(ScenarioViewModel scenario, CancellationToken ct)
    {
        await _repository.DeleteAsync(scenario.Model.Id, ct);

        var wasSelected = SelectedScenario == scenario;
        Scenarios.Remove(scenario);

        if (wasSelected)
            SelectedScenario = Scenarios.FirstOrDefault();
    }
}
