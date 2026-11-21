using System.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace NetBench.Desktop;

/// <summary>
/// Автообновление установленной (через Velopack) desktop-версии из GitHub Releases:
/// новая версия качается в фоне и ставится после выхода из приложения — следующий запуск
/// уже на ней. Запуск из IDE / dotnet run (приложение не установлено) обновления не трогает.
/// В сборку для Mac App Store не входит — там <c>StoreUpdates.cs</c>.
/// </summary>
internal static class Updates
{
    private const string RepositoryUrl = "https://github.com/Plumix-Net/NetBench";

    /// <summary>Первой строкой <c>Main</c>: установщик/апдейтер Velopack запускает exe с хуками и ждёт быстрого выхода.</summary>
    public static void Start()
    {
        VelopackApp.Build().Run();
        _ = Task.Run(DownloadInBackgroundAsync);
    }

    private static async Task DownloadInBackgroundAsync()
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
