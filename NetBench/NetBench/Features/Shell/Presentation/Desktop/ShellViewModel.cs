using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Report.Presentation.Desktop;
using NetBench.Features.Scenarios.Presentation.Desktop;
using NetBench.Features.TestRun.Presentation.Desktop;
using NetBench.Desktop.Services;

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

    public bool ShowTargetHint => ActiveScenario is { HasTarget: false };

    [RelayCommand(CanExecute = nameof(CanRun))]
    private void Run()
    {
        if (ActiveScenario is { } scenario)
            Sidebar.RunScenarioCommand.Execute(scenario);
    }

    private bool CanRun() => ActiveScenario is { HasTarget: true } && !IsRunning;

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Stop()
    {
        if (CurrentPage is TestRunViewModel run)
            run.StopCommand.Execute(null);
    }

    // ---- Подписки ----

    private void OnPageChanged(ObservableObject? page)
    {
        if (_observedRun is not null)
            _observedRun.PropertyChanged -= OnRunPropertyChanged;

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
        RunCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
    }
}
