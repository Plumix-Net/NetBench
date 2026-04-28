using System.ComponentModel;

namespace NetBench.Core.Models;

public sealed class LoadScenario : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _target = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
    }

    public string Target
    {
        get => _target;
        set { if (_target != value) { _target = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Target))); } }
    }

    public List<RequestStep> Requests { get; init; } = [];
    public LoadConfig Load { get; init; } = new();
}

public sealed class RequestStep
{
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/";
    public Dictionary<string, string> Headers { get; init; } = [];
    public string? Body { get; set; }
}

public sealed class LoadConfig
{
    public int ConcurrentUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RampUp { get; set; } = TimeSpan.Zero;
    public TimeSpan ThinkTime { get; set; } = TimeSpan.Zero;
}
