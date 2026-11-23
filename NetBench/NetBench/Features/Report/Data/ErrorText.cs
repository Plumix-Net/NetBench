using NetBench.Features.TestRun.Domain;
using NetBench.Localization;

namespace NetBench.Features.Report.Data;

/// <summary>Причины запросов без ответа — для людей (подпись, подсказка) и для файлов экспорта (id).</summary>
public static class ErrorText
{
    public static string Label(RequestErrorKind kind)
    {
        var s = Strings.Instance.Root.Report.ErrorKind;
        return kind switch
        {
            RequestErrorKind.Dns => s.Dns,
            RequestErrorKind.ConnectionRefused => s.ConnectionRefused,
            RequestErrorKind.Connection => s.Connection,
            RequestErrorKind.Timeout => s.Timeout,
            RequestErrorKind.Tls => s.Tls,
            RequestErrorKind.Protocol => s.Protocol,
            RequestErrorKind.InvalidRequest => s.InvalidRequest,
            _ => s.Other,
        };
    }

    public static string Hint(RequestErrorKind kind)
    {
        var s = Strings.Instance.Root.Report.ErrorHint;
        return kind switch
        {
            RequestErrorKind.Dns => s.Dns,
            RequestErrorKind.ConnectionRefused => s.ConnectionRefused,
            RequestErrorKind.Connection => s.Connection,
            RequestErrorKind.Timeout => s.Timeout,
            RequestErrorKind.Tls => s.Tls,
            RequestErrorKind.Protocol => s.Protocol,
            RequestErrorKind.InvalidRequest => s.InvalidRequest,
            _ => s.Other,
        };
    }

    /// <summary>Стабильный машинный id причины — не зависит от языка интерфейса.</summary>
    public static string Id(RequestErrorKind kind) => kind switch
    {
        RequestErrorKind.Dns => "dns",
        RequestErrorKind.ConnectionRefused => "connection_refused",
        RequestErrorKind.Connection => "connection",
        RequestErrorKind.Timeout => "timeout",
        RequestErrorKind.Tls => "tls",
        RequestErrorKind.Protocol => "protocol",
        RequestErrorKind.InvalidRequest => "invalid_request",
        _ => "other",
    };

    /// <summary>Размер в человеческих единицах: 512 B, 4.2 KB, 13.0 MB.</summary>
    public static string Bytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes:N0} B",
        < 1024 * 1024 => $"{bytes / 1024.0:N1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):N1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):N2} GB",
    };
}
