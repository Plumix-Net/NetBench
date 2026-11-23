using System.Net;
using System.Net.Sockets;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Data;
using NetBench.Features.TestRun.Domain;
using Xunit;

namespace NetBench.Tests;

/// <summary>Движок против своих серверов на loopback — без внешней сети.</summary>
public class LoadEngineTests
{
    private static LoadScenario Scenario(string target, int users = 2, double seconds = 1, string method = "GET") => new()
    {
        Name = "test",
        Target = target,
        Requests = [new RequestStep { Method = method, Path = "/" }],
        Load = new LoadConfig { ConcurrentUsers = users, Duration = TimeSpan.FromSeconds(seconds) },
    };

    private static async Task<TestRunReport> RunAsync(LoadScenario scenario)
    {
        await using var engine = new LoadEngine();
        TestRunReport? report = null;
        engine.RunCompleted += r => report = r;
        try
        {
            await engine.RunAsync(scenario);
        }
        catch (OperationCanceledException)
        {
            // прогон остановил сам движок (битый запрос) — отчёт всё равно есть
        }

        return Assert.IsType<TestRunReport>(report);
    }

    [Fact]
    public async Task TargetWithoutSchemeIsRejectedBeforeAnyRequest()
    {
        await using var engine = new LoadEngine();

        await Assert.ThrowsAsync<ArgumentException>(() => engine.RunAsync(Scenario("google.com")));
    }

    [Fact]
    public async Task ClosedPortIsReportedAsConnectionRefusedWithoutLatency()
    {
        var report = await RunAsync(Scenario($"http://127.0.0.1:{FreePort()}/"));

        Assert.True(report.Summary.TotalRequests > 0);
        Assert.Equal(report.Summary.TotalRequests, report.Summary.ErrorCount);
        Assert.Equal(0, report.Summary.RespondedRequests);
        Assert.Equal(0, report.Summary.LatencyP95Ms);
        var error = Assert.Single(report.Errors);
        Assert.Equal(RequestErrorKind.ConnectionRefused, error.Kind);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.Equal(RequestErrorKind.ConnectionRefused, report.Summary.TopError?.Kind);
    }

    [Fact]
    public async Task InvalidRequestStopsTheRunImmediately()
    {
        using var server = new LoopbackServer();
        var started = DateTime.UtcNow;

        var report = await RunAsync(Scenario(server.Url, seconds: 30, method: "BAD METHOD"));

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10));
        Assert.Equal(RequestErrorKind.InvalidRequest, Assert.Single(report.Errors).Kind);
    }

    [Fact]
    public async Task ResponsesFeedLatencyAndLiveThroughput()
    {
        using var server = new LoopbackServer();
        var engine = new LoadEngine();
        var snapshots = new List<TestRunStats>();
        engine.StatsUpdated += s => { lock (snapshots) snapshots.Add(s); };
        TestRunReport? report = null;
        engine.RunCompleted += r => report = r;

        await engine.RunAsync(Scenario(server.Url, users: 4, seconds: 1.5));
        await engine.DisposeAsync();

        Assert.NotNull(report);
        Assert.Equal(0, report.Summary.ErrorCount);
        Assert.Equal(report.Summary.TotalRequests, report.Summary.RespondedRequests);
        Assert.True(report.Summary.LatencyP95Ms > 0);
        Assert.Empty(report.Errors);
        lock (snapshots)
        {
            Assert.Contains(snapshots, s => s.CurrentRequestsPerSecond > 0 && s.ActiveUsers == 4);
        }
    }

    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    private sealed class LoopbackServer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public LoopbackServer()
        {
            Url = $"http://127.0.0.1:{FreePort()}/";
            _listener.Prefixes.Add(Url);
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public string Url { get; }

        public void Dispose() => _listener.Close();

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    return;
                }
            }
        }
    }
}
