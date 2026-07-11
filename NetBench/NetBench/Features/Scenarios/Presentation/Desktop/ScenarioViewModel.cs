using CommunityToolkit.Mvvm.ComponentModel;
using NetBench.Features.Scenarios.Domain;

namespace NetBench.Features.Scenarios.Presentation.Desktop;

public sealed partial class ScenarioViewModel : ObservableObject
{
    public LoadScenario Model { get; }

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _target;

    public ScenarioViewModel(LoadScenario model)
    {
        Model = model;
        _name = model.Name;
        _target = model.Target;
    }

    partial void OnNameChanged(string value) => Model.Name = value;
    partial void OnTargetChanged(string value) => Model.Target = value;
}
