using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Scenarios.Domain;
using NetBench.Desktop.Services;
using NetBench.Localization;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public partial class ScenarioEditorViewModel : ObservableObject
{
    public static IReadOnlyList<string> HttpMethods { get; } = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    private readonly IScenarioRepository _repository;

    public ScenarioViewModel Scenario { get; }
    public ObservableCollection<RequestStep> Requests { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderText))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTargetError))]
    private string _target = string.Empty;

    [ObservableProperty] private int _concurrentUsers;
    [ObservableProperty] private int _durationSeconds;
    [ObservableProperty] private int _rampUpSeconds;
    [ObservableProperty] private int _thinkTimeMs;

    public ScenarioEditorViewModel(ScenarioViewModel scenario, IScenarioRepository repository)
    {
        Scenario = scenario;
        _repository = repository;
        ResetFromModel();
    }

    public string HeaderText => string.IsNullOrWhiteSpace(Name)
        ? Strings.Instance.Root.Scenarios.New
        : Name;

    public bool HasTargetError => string.IsNullOrWhiteSpace(Target);

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        Scenario.Name = string.IsNullOrWhiteSpace(Name)
            ? Strings.Instance.Root.Scenarios.Unnamed
            : Name.Trim();
        Scenario.Target = Target.Trim();
        Name = Scenario.Name;
        Target = Scenario.Target;

        var model = Scenario.Model;
        model.Load.ConcurrentUsers = ConcurrentUsers;
        model.Load.Duration = TimeSpan.FromSeconds(DurationSeconds);
        model.Load.RampUp = TimeSpan.FromSeconds(RampUpSeconds);
        model.Load.ThinkTime = TimeSpan.FromMilliseconds(ThinkTimeMs);

        model.Requests.Clear();
        foreach (var req in Requests)
            model.Requests.Add(Clone(req));

        await _repository.SaveAsync(model, ct);
    }

    /// <summary>Откат черновика к сохранённому состоянию сценария.</summary>
    [RelayCommand]
    private void Cancel() => ResetFromModel();

    [RelayCommand]
    private void AddRequest() =>
        Requests.Add(new RequestStep { Method = "GET", Path = "/" });

    [RelayCommand]
    private void RemoveRequest(RequestStep step) =>
        Requests.Remove(step);

    private void ResetFromModel()
    {
        var model = Scenario.Model;
        Name = model.Name;
        Target = model.Target;
        ConcurrentUsers = model.Load.ConcurrentUsers;
        DurationSeconds = (int)model.Load.Duration.TotalSeconds;
        RampUpSeconds = (int)model.Load.RampUp.TotalSeconds;
        ThinkTimeMs = (int)model.Load.ThinkTime.TotalMilliseconds;

        // Редактируем копии шагов — модель меняется только при сохранении.
        Requests.Clear();
        foreach (var req in model.Requests)
            Requests.Add(Clone(req));
    }

    private static RequestStep Clone(RequestStep step) => new()
    {
        Method = step.Method,
        Path = step.Path,
        Headers = new Dictionary<string, string>(step.Headers),
        Body = step.Body,
    };
}
