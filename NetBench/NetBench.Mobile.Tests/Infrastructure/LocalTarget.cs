using System.Net;
using System.Net.Sockets;

namespace NetBench.Mobile.Tests.Infrastructure;

/// <summary>Свой сервер на loopback — цель прогона, как и требует RunConsent: «только свои серверы».</summary>
public sealed class LocalTarget : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Task _loop;

    public LocalTarget()
    {
        Url = $"http://127.0.0.1:{FreePort()}/";
        _listener.Prefixes.Add(Url);
        _listener.Start();
        _loop = Task.Run(ServeAsync);
    }

    public string Url { get; }

    public void Dispose()
    {
        _listener.Close();
        _ = _loop.ContinueWith(static _ => { }, TaskScheduler.Default);
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                return;
            }

            context.Response.StatusCode = 200;
            context.Response.Close();
        }
    }

    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }
}
