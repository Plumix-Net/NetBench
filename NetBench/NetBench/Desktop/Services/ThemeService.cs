using Avalonia;
using Avalonia.Styling;

namespace NetBench.Desktop.Services;

internal sealed class ThemeService : IThemeService
{
    public event Action? Changed;

    public bool IsDark =>
        Application.Current?.ActualThemeVariant != ThemeVariant.Light;

    public void Toggle()
    {
        if (Application.Current is not { } app)
            return;

        app.RequestedThemeVariant = IsDark ? ThemeVariant.Light : ThemeVariant.Dark;
        Changed?.Invoke();
    }
}
