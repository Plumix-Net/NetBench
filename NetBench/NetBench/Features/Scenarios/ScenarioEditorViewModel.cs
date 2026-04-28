using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Core.Models;
using NetBench.Core.Storage;
using NetBench.Services;

namespace NetBench.Features.Scenarios;

public partial class ScenarioEditorViewModel : ObservableObject
{
    private readonly ScenarioRepository _repository;
    private readonly INavigationService _navigation;

    public LoadScenario Scenario { get; }
    public ObservableCollection<RequestStep> Requests { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _target = string.Empty;
    [ObservableProperty] private int _concurrentUsers;
    [ObservableProperty] private int _durationSeconds;
    [ObservableProperty] private int _rampUpSeconds;
    [ObservableProperty] private int _thinkTimeMs;

    public ScenarioEditorViewModel(
        LoadScenario scenario,
        ScenarioRepository repository,
        INavigationService navigation)
    {
        Scenario = scenario;
        _repository = repository;
        _navigation = navigation;

        _name = scenario.Name;
        _target = scenario.Target;
        _concurrentUsers = scenario.Load.ConcurrentUsers;
        _durationSeconds = (int)scenario.Load.Duration.TotalSeconds;
        _rampUpSeconds = (int)scenario.Load.RampUp.TotalSeconds;
        _thinkTimeMs = (int)scenario.Load.ThinkTime.TotalMilliseconds;

        Requests = new ObservableCollection<RequestStep>(scenario.Requests);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        Scenario.Name = Name;
        Scenario.Target = Target;
        Scenario.Load.ConcurrentUsers = ConcurrentUsers;
        Scenario.Load.Duration = TimeSpan.FromSeconds(DurationSeconds);
        Scenario.Load.RampUp = TimeSpan.FromSeconds(RampUpSeconds);
        Scenario.Load.ThinkTime = TimeSpan.FromMilliseconds(ThinkTimeMs);

        Scenario.Requests.Clear();
        foreach (var req in Requests)
            Scenario.Requests.Add(req);

        await _repository.SaveAsync(Scenario, ct);
    }

    [RelayCommand]
    private void AddRequest() =>
        Requests.Add(new RequestStep { Method = "GET", Path = "/" });

    [RelayCommand]
    private void RemoveRequest(RequestStep step) =>
        Requests.Remove(step);

    [RelayCommand]
    private void Cancel() => _navigation.NavigateTo(null);
}
