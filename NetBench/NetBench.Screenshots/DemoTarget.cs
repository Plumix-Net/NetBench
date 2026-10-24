using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetBench.Screenshots;

/// <summary>
/// «Сервер» для живого прогона на скриншоте: HTTP-прокси на loopback, который сам отвечает на любой
/// запрос с правдоподобной задержкой и редкими 503. Через переменную http_proxy на него уходят
/// запросы к адресу из сценария (например, http://192.168.1.10:8080) — в кадре реальный адрес
/// стенда, а в сеть не уходит ни одного пакета.
/// </summary>
internal sealed class DemoTarget : IDisposable
{
    private static readonly byte[] _okBody = Encoding.UTF8.GetBytes("""{"items":[{"id":1042,"sku":"NB-1042","price":19.9}],"total":1}""");
    private static readonly byte[] _ok = Response("200 OK", _okBody);
    private static readonly byte[] _unavailable = Response("503 Service Unavailable", "{}"u8.ToArray());

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();

    public DemoTarget()
    {
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = AcceptAsync();
    }

    public int Port { get; }

    /// <summary>Направляет HTTP-трафик процесса на этот сервер; вызывать до первого HttpClient.</summary>
    public void UseAsProxy()
    {
        var proxy = $"http://127.0.0.1:{Port}";
        Environment.SetEnvironmentVariable("http_proxy", proxy);
        Environment.SetEnvironmentVariable("HTTP_PROXY", proxy);
        Environment.SetEnvironmentVariable("no_proxy", null);
        Environment.SetEnvironmentVariable("NO_PROXY", null);
    }

    private async Task AcceptAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _ = ServeAsync(client);
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using var _ = client;
        var stream = client.GetStream();
        var buffer = new byte[8192];
        var pending = 0;
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                // Тела у GET нет: запрос кончается пустой строкой после заголовков.
                int end;
                while ((end = buffer.AsSpan(0, pending).IndexOf("\r\n\r\n"u8)) < 0)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(pending), _cts.Token);
                    if (read == 0)
                        return;
                    pending += read;
                }

                var consumed = end + 4;
                buffer.AsSpan(consumed, pending - consumed).CopyTo(buffer);
                pending -= consumed;

                await Task.Delay(Latency(), _cts.Token);
                await stream.WriteAsync(Random.Shared.NextDouble() < 0.004 ? _unavailable : _ok, _cts.Token);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
        }
    }

    /// <summary>Типичный API под нагрузкой: ~30–45 мс, хвост до ~200 мс у пары процентов запросов.</summary>
    private static TimeSpan Latency()
    {
        var ms = 28 + Random.Shared.NextDouble() * 18;
        if (Random.Shared.NextDouble() < 0.03)
            ms += 60 + Random.Shared.NextDouble() * 140;
        return TimeSpan.FromMilliseconds(ms);
    }

    private static byte[] Response(string status, byte[] body) =>
    [
        .. Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {status}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\nConnection: keep-alive\r\n\r\n"),
        .. body,
    ];

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
    }
}
