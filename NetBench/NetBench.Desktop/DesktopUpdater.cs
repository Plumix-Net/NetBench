using System.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace NetBench.Desktop;

/// <summary>
/// Автообновление установленной (через Velopack) desktop-версии из GitHub Releases:
/// новая версия качается в фоне и ставится после выхода из приложения — следующий запуск
/// уже на ней. Запуск из IDE / dotnet run (приложение не установлено) обновления не трогает.
/// </summary>
internal static class DesktopUpdater
{
    private const string RepositoryUrl = "https://github.com/Plumix-Net/NetBench";

    public static async Task DownloadInBackgroundAsync()
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
            if (!manager.IsInstalled)
                return;

            var update = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (update is null)
                return;

            await manager.DownloadUpdatesAsync(update).ConfigureAwait(false);
            manager.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: true, restart: false);
        }
        catch (Exception exception)
        {
            // Нет сети, лимит GitHub API и т.п. — обновимся при следующем запуске.
            Trace.WriteLine($"Update check failed: {exception}");
        }
    }
}
