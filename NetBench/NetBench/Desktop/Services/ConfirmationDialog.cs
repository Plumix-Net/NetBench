using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using NetBench.Desktop.Views;

namespace NetBench.Desktop.Services;

internal sealed class ConfirmationDialog : IConfirmationDialog
{
    public async Task<bool> ConfirmAsync(string title, string message, string confirm, string cancel)
    {
        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null)
            return false;

        var dialog = new ConfirmDialog(title, message, confirm, cancel);
        return await dialog.ShowDialog<bool>(owner);
    }
}
