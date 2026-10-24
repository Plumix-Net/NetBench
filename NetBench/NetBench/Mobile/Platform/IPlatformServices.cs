using NetBench.Features.Report.Domain;

namespace NetBench.Mobile.Platform;

/// <summary>
/// Возможности ОС, которых нет в Plumix: меню «Поделиться», запрет гасить экран и открытие ссылок.
/// Реализации живут в головах (NetBench.Android/NetBench.iOS) и ставятся в <see cref="PlatformServices.Current"/>
/// до старта приложения; общий код получает их через RepositoryProvider из MobileShell.
/// </summary>
public interface IPlatformServices : IShareSheet
{
    /// <summary>Не гасить экран, пока идёт прогон: со сном устройства ОС душит сеть приложения.</summary>
    void SetKeepScreenOn(bool keepOn);

    /// <returns><c>false</c>, если открыть ссылку нечем.</returns>
    Task<bool> OpenUrlAsync(Uri uri);
}

public static class PlatformServices
{
    public static IPlatformServices Current { get; set; } = new UnsupportedPlatformServices();
}

/// <summary>
/// Заглушка для хостов без реализации (headless-проверки, desktop-окно Plumix): ничего не делает,
/// а «Поделиться» и ссылки честно сообщают о неудаче.
/// </summary>
internal sealed class UnsupportedPlatformServices : IPlatformServices
{
    public Task ShareTextAsync(string text, string title) =>
        Task.FromException(new PlatformNotSupportedException());

    public Task ShareFileAsync(string path, string mimeType, string title) =>
        Task.FromException(new PlatformNotSupportedException());

    public void SetKeepScreenOn(bool keepOn)
    {
    }

    public Task<bool> OpenUrlAsync(Uri uri) => Task.FromResult(false);
}
