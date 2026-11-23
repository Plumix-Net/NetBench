using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Desktop.Services;
using NetBench.Localization;

namespace NetBench.Features.Shell.Presentation.Desktop;

public partial class ShellViewModel : ObservableObject
{
    public ScenarioListViewModel Sidebar { get; }

    [ObservableProperty]
    private ObservableObject? _currentPage;

    private ScenarioViewModel? _observedScenario;
    private TestRunViewModel? _observedRun;

    public ShellViewModel(INavigationService navigation, ScenarioListViewModel sidebar)
    {
        Sidebar = sidebar;

        navigation.PageChanged += OnPageChanged;
        sidebar.PropertyChanged += OnSidebarPropertyChanged;
        HookScenarios(sidebar.Scenarios);

        _ = sidebar.LoadCommand.ExecuteAsync(null);
    }

    // ---- Состояние нижней панели запуска ----

    public bool IsEmpty => Sidebar.Scenarios.Count == 0;

    public ScenarioViewModel? ActiveScenario => Sidebar.SelectedScenario;

    public bool IsRunBarVisible => ActiveScenario is not null && CurrentPage is not CompareViewModel;

    public bool IsRunning => CurrentPage is TestRunViewModel { IsRunning: true };

    public bool ShowRunButton => !IsRunning;

    public bool ShowTargetHint => ActiveScenario is { HasValidTarget: false };

    public string TargetHintText => ActiveScenario is { HasTarget: true }
        ? Strings.Instance.Root.Shell.TargetInvalid
        : Strings.Instance.Root.Shell.TargetRequired;

    public bool IsReportOpen => CurrentPage is ReportViewModel;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        if (ActiveScenario is not { } scenario)
            return;

        // Запуск из открытого редактора — с тем, что в нём набрано: сначала сохраняем черновик.
        // Невалидный адрес не сохранится, и редактор уже показывает, что с ним не так.
        if (CurrentPage is ScenarioEditorViewModel editor && editor.Scenario == scenario &&
            !await editor.TrySaveAsync())
            return;

        await Sidebar.RunScenarioCommand.ExecuteAsync(scenario);
    }

    private bool CanRun() => ActiveScenario is not null && !IsRunning;

    /// <summary>Редактор выбранного сценария — из отчёта и меню (кнопки строки сайдбара видны только при наведении).</summary>
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void EditScenario()
    {
        if (ActiveScenario is { } scenario)
            Sidebar.EditScenarioCommand.Execute(scenario);
    }

    private bool CanEdit() => ActiveScenario is not null && !IsRunning;

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop()
    {
        if (CurrentPage is TestRunViewModel run)
            run.StopCommand.Execute(null);
    }

    // ---- Подписки ----

    // ---- Меню: экспорт открытого отчёта ----

    [RelayCommand(CanExecute = nameof(IsReportOpen))]
    private Task ExportJsonAsync() => (CurrentPage as ReportViewModel)?.ExportJsonCommand.ExecuteAsync(null) ?? Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(IsReportOpen))]
    private Task ExportHtmlAsync() => (CurrentPage as ReportViewModel)?.ExportHtmlCommand.ExecuteAsync(null) ?? Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(IsReportOpen))]
    private Task ExportCsvAsync() => (CurrentPage as ReportViewModel)?.ExportCsvCommand.ExecuteAsync(null) ?? Task.CompletedTask;

    private void OnPageChanged(ObservableObject? page)
    {
        if (_observedRun is not null)
        {
            _observedRun.PropertyChanged -= OnRunPropertyChanged;

            // Ушли с экрана прогона (выбрали другой сценарий) — нагрузка не должна идти дальше
            // невидимой: останавливаем, отчёт сохранится в историю.
            if (_observedRun.IsRunning && !ReferenceEquals(page, _observedRun))
                _observedRun.StopCommand.Execute(null);
        }

        CurrentPage = page;

        _observedRun = page as TestRunViewModel;
        if (_observedRun is not null)
            _observedRun.PropertyChanged += OnRunPropertyChanged;

        RefreshRunBar();
    }

    private void OnSidebarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ScenarioListViewModel.SelectedScenario):
                if (_observedScenario is not null)
                    _observedScenario.PropertyChanged -= OnScenarioPropertyChanged;
                _observedScenario = Sidebar.SelectedScenario;
                if (_observedScenario is not null)
                    _observedScenario.PropertyChanged += OnScenarioPropertyChanged;
                OnPropertyChanged(nameof(ActiveScenario));
                RefreshRunBar();
                break;

            case nameof(ScenarioListViewModel.Scenarios):
                HookScenarios(Sidebar.Scenarios);
                OnPropertyChanged(nameof(IsEmpty));
                break;
        }
    }

    private void HookScenarios(ObservableCollection<ScenarioViewModel> scenarios) =>
        scenarios.CollectionChanged += OnScenariosCollectionChanged;

    private void OnScenariosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(IsEmpty));

    private void OnScenarioPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ScenarioViewModel.Target) or nameof(ScenarioViewModel.HasTarget))
            RefreshRunBar();
    }

    private void OnRunPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestRunViewModel.IsRunning))
            RefreshRunBar();
    }

    private void RefreshRunBar()
    {
        OnPropertyChanged(nameof(IsRunBarVisible));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(ShowRunButton));
        OnPropertyChanged(nameof(ShowTargetHint));
        OnPropertyChanged(nameof(TargetHintText));
        OnPropertyChanged(nameof(IsReportOpen));
        RunCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        EditScenarioCommand.NotifyCanExecuteChanged();
        ExportJsonCommand.NotifyCanExecuteChanged();
        ExportHtmlCommand.NotifyCanExecuteChanged();
        ExportCsvCommand.NotifyCanExecuteChanged();
    }
}
