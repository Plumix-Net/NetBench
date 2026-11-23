namespace NetBench.Features.TestRun.Domain;

public readonly struct RequestResult
{
    public readonly long StartTimestampNs;
    public readonly long EndTimestampNs;
    public readonly int StatusCode;
    public readonly int BytesReceived;
    public readonly bool IsError;

    /// <summary>Почему запрос не получил ответа; для ответов (в том числе 4xx/5xx) — <see cref="RequestErrorKind.None"/>.</summary>
    public readonly RequestErrorKind ErrorKind;

    public long LatencyNs => EndTimestampNs - StartTimestampNs;
    public double LatencyMs => LatencyNs / 1_000_000.0;

    /// <summary>Сервер ответил (любым кодом) — задержка такого запроса и есть время ответа сервера.</summary>
    public bool HasResponse => StatusCode != 0;

    public RequestResult(long startNs, long endNs, int statusCode, int bytesReceived, bool isError)
    {
        StartTimestampNs = startNs;
        EndTimestampNs = endNs;
        StatusCode = statusCode;
        BytesReceived = bytesReceived;
        IsError = isError;
        ErrorKind = RequestErrorKind.None;
    }

    private RequestResult(long startNs, long endNs, RequestErrorKind kind)
    {
        StartTimestampNs = startNs;
        EndTimestampNs = endNs;
        StatusCode = 0;
        BytesReceived = 0;
        IsError = true;
        ErrorKind = kind;
    }

    /// <summary>Запрос без ответа: сетевой сбой, тайм-аут, битый запрос — код 0.</summary>
    public static RequestResult Error(long startNs, long endNs, RequestErrorKind kind = RequestErrorKind.Other) =>
        new(startNs, endNs, kind == RequestErrorKind.None ? RequestErrorKind.Other : kind);
}

/// <summary>Причина, по которой запрос остался без ответа сервера.</summary>
public enum RequestErrorKind : byte
{
    None = 0,

    /// <summary>Имя хоста не разрешилось (DNS).</summary>
    Dns,

    /// <summary>Хост отверг соединение: порт закрыт, сервер не запущен.</summary>
    ConnectionRefused,

    /// <summary>Соединение не установилось или оборвалось по другой сетевой причине.</summary>
    Connection,

    /// <summary>Не уложились в тайм-аут подключения или ответа.</summary>
    Timeout,

    /// <summary>Сбой TLS-рукопожатия: сертификат, протокол.</summary>
    Tls,

    /// <summary>Сервер ответил не по HTTP или оборвал ответ.</summary>
    Protocol,

    /// <summary>Запрос нельзя отправить: битый URL, метод или заголовок. Прогон останавливается сразу.</summary>
    InvalidRequest,

    /// <summary>Всё остальное.</summary>
    Other,
}
