using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Scenarios.Domain;
using NetBench.Desktop.Services;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public partial class ScenarioEditorViewModel : ObservableObject
{
    private readonly IScenarioRepository _repository;
    private readonly INavigationService _navigation;

    public ScenarioViewModel Scenario { get; }
    public ObservableCollection<RequestStep> Requests { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _target = string.Empty;
    [ObservableProperty] private int _concurrentUsers;
    [ObservableProperty] private int _durationSeconds;
    [ObservableProperty] private int _rampUpSeconds;
    [ObservableProperty] private int _thinkTimeMs;

    public ScenarioEditorViewModel(
        ScenarioViewModel scenario,
        IScenarioRepository repository,
        INavigationService navigation)
    {
        Scenario = scenario;
        _repository = repository;
        _navigation = navigation;

        var model = scenario.Model;
        _name = model.Name;
        _target = model.Target;
        _concurrentUsers = model.Load.ConcurrentUsers;
        _durationSeconds = (int)model.Load.Duration.TotalSeconds;
        _rampUpSeconds = (int)model.Load.RampUp.TotalSeconds;
        _thinkTimeMs = (int)model.Load.ThinkTime.TotalMilliseconds;

        Requests = new ObservableCollection<RequestStep>(model.Requests);
        if (Requests.Count == 0)
            Requests.Add(new RequestStep { Method = "GET", Path = "/" });
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        Scenario.Name = Name;
        Scenario.Target = Target;

        var model = Scenario.Model;
        model.Load.ConcurrentUsers = ConcurrentUsers;
        model.Load.Duration = TimeSpan.FromSeconds(DurationSeconds);
        model.Load.RampUp = TimeSpan.FromSeconds(RampUpSeconds);
        model.Load.ThinkTime = TimeSpan.FromMilliseconds(ThinkTimeMs);

        model.Requests.Clear();
        foreach (var req in Requests)
            model.Requests.Add(req);

        await _repository.SaveAsync(model, ct);
        _navigation.NavigateTo(null);
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
