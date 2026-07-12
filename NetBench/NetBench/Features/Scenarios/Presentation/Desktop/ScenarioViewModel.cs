using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Scenarios.Domain;
using NetBench.Localization;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public sealed partial class ScenarioViewModel : ObservableObject
{
    public LoadScenario Model { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HostLabel))]
    [NotifyPropertyChangedFor(nameof(TargetLabel))]
    [NotifyPropertyChangedFor(nameof(HasTarget))]
    private string _target;

    public ScenarioViewModel(LoadScenario model)
    {
        Model = model;
        _name = model.Name;
        _target = model.Target;
    }

    public bool HasTarget => !string.IsNullOrWhiteSpace(Target);

    /// <summary>Хост без схемы — подпись строки в сайдбаре.</summary>
    public string HostLabel => HasTarget
        ? Target.Replace("https://", "").Replace("http://", "")
        : Strings.Instance.Root.Scenarios.TargetNotSetParenthesized;

    /// <summary>Target для нижней панели запуска.</summary>
    public string TargetLabel => HasTarget
        ? Target
        : Strings.Instance.Root.Scenarios.TargetNotSetParenthesized;

    partial void OnNameChanged(string value) => Model.Name = value;
    partial void OnTargetChanged(string value) => Model.Target = value;
}
