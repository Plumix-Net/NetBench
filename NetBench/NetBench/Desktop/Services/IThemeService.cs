namespace NetBench.Desktop.Services;

public interface IThemeService
{
    bool IsDark { get; }
    event Action? Changed;
    void Toggle();
}
