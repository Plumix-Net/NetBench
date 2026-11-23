using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetBench.Features.Scenarios.Domain;
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
    [NotifyPropertyChangedFor(nameof(TargetError))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _target = string.Empty;

    /// <summary>Черновик сохранён и с тех пор не менялся — показываем «Сохранено».</summary>
    [ObservableProperty] private bool _justSaved;

    private bool _resetting;

    [ObservableProperty] private int _concurrentUsers;
    [ObservableProperty] private int _durationSeconds;
    [ObservableProperty] private int _rampUpSeconds;
    [ObservableProperty] private int _thinkTimeMs;

    public ScenarioEditorViewModel(ScenarioViewModel scenario, IScenarioRepository repository)
    {
        Scenario = scenario;
        _repository = repository;
        ResetFromModel();
        Requests.CollectionChanged += (_, _) => JustSaved = false;
    }

    public string HeaderText => string.IsNullOrWhiteSpace(Name)
        ? Strings.Instance.Root.Scenarios.New
        : Name;

    /// <summary>
    /// Адрес можно запросить: схему разрешено не писать (подставим https://), но мусор
    /// вроде «google» или «ftp://…» не пропускаем — с ним прогон состоял бы из одних ошибок.
    /// </summary>
    public bool HasTargetError => !TargetUrl.TryNormalize(Target, out _);

    public string TargetError => string.IsNullOrWhiteSpace(Target)
        ? Strings.Instance.Root.Scenarios.Editor.TargetRequired
        : Strings.Instance.Root.Scenarios.Editor.TargetInvalid;

    /// <summary>Сохранить черновик; <c>false</c> — адрес невалиден, ничего не записано.</summary>
    public async Task<bool> TrySaveAsync(CancellationToken ct = default)
    {
        if (!CanSave())
            return false;
        await SaveAsync(ct);
        return true;
    }

    private bool CanSave() => !HasTargetError;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (!_resetting && e.PropertyName is nameof(Name) or nameof(Target) or nameof(ConcurrentUsers)
                or nameof(DurationSeconds) or nameof(RampUpSeconds) or nameof(ThinkTimeMs))
            JustSaved = false;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct)
    {
        if (!TargetUrl.TryNormalize(Target, out var target))
            return;

        Scenario.Name = string.IsNullOrWhiteSpace(Name)
            ? Strings.Instance.Root.Scenarios.Unnamed
            : Name.Trim();
        Scenario.Target = target;
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
        JustSaved = true;
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
        _resetting = true;
        try
        {
            ResetFromModelCore();
        }
        finally
        {
            _resetting = false;
        }
    }

    private void ResetFromModelCore()
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
