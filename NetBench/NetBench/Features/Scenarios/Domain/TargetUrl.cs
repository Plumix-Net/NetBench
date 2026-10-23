using System.Diagnostics.CodeAnalysis;

namespace NetBench.Features.Scenarios.Domain;

/// <summary>
/// Проверка Target URL перед запуском: движок бьёт в него как есть, и с битым адресом
/// прогон честно отработает всю длительность одними ошибками — лучше не пускать.
/// </summary>
public static class TargetUrl
{
    /// <summary>Абсолютный http(s)-адрес с хостом — то, что движок сможет запросить.</summary>
    public static bool IsValid([NotNullWhen(true)] string? target) =>
        Uri.TryCreate(target?.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        uri.Host.Length > 0;

    /// <summary>
    /// Ввод пользователя → валидный URL: схему можно не писать, по умолчанию https.
    /// Явная чужая схема (ftp:// и т.п.) не переписывается, а отклоняется.
    /// </summary>
    public static bool TryNormalize(string? input, [NotNullWhen(true)] out string? url)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length > 0 && !trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;

        url = IsValid(trimmed) ? trimmed : null;
        return url is not null;
    }
}
