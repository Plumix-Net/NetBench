using NetBench.Core.Models;
using NetBench.Core.Statistics;
using System.Buffers;
using System.Diagnostics;
using System.Threading.Channels;

namespace NetBench.Core.Engine;

public sealed class LoadEngine : IAsyncDisposable
{
    private static readonly TimeSpan StatsInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;
    private readonly Channel<RequestResult> _channel;
    private readonly StatisticsAggregator _aggregator;

    private Task? _consumerTask;
    private CancellationTokenSource? _cts;

    public event Action<TestRunStats>? StatsUpdated;
    public event Action<TestRunReport>? RunCompleted;

    public LoadEngine()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _channel = Channel.CreateBounded<RequestResult>(new BoundedChannelOptions(65536)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _aggregator = new StatisticsAggregator();
    }

    public async Task RunAsync(LoadScenario scenario, CancellationToken externalToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = _cts.Token;
        var startedAt = DateTime.UtcNow;

        _consumerTask = Task.Run(ConsumeResultsAsync);

        var statsTask = Task.Run(() => EmitStatsAsync(token), token);

        try
        {
            await RunLoadAsync(scenario, token).ConfigureAwait(false);
        }
        finally
        {
            _channel.Writer.TryComplete();
            await (_consumerTask ?? Task.CompletedTask).ConfigureAwait(false);

            _cts.Cancel();
            await statsTask.ConfigureAwait(false);

            var report = _aggregator.BuildReport(scenario, startedAt);
            RunCompleted?.Invoke(report);
        }
    }

    public void Stop() => _cts?.Cancel();

    private async Task RunLoadAsync(LoadScenario scenario, CancellationToken token)
    {
        var config = scenario.Load;
        var concurrency = config.ConcurrentUsers;

        // Ramp-up: gradually add workers over ramp-up duration
        var rampUpDelay = config.RampUp.TotalMilliseconds > 0 && concurrency > 1
            ? (int)(config.RampUp.TotalMilliseconds / concurrency)
            : 0;

        var deadline = Stopwatch.GetTimestamp() +
                       (long)(config.Duration.TotalSeconds * Stopwatch.Frequency);

        using var semaphore = new SemaphoreSlim(concurrency, concurrency);
        var tasks = new List<Task>(concurrency);

        for (var i = 0; i < concurrency && !token.IsCancellationRequested; i++)
        {
            if (rampUpDelay > 0)
                await Task.Delay(rampUpDelay, token).ConfigureAwait(false);

            var workerIndex = i;
            tasks.Add(Task.Run(() => WorkerAsync(scenario, deadline, workerIndex, token), token));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task WorkerAsync(LoadScenario scenario, long deadlineTimestamp, int workerIndex, CancellationToken token)
    {
        if (scenario.Requests.Count == 0)
            return;

        while (!token.IsCancellationRequested &&
               Stopwatch.GetTimestamp() < deadlineTimestamp)
        {
            foreach (var step in scenario.Requests)
            {
                if (token.IsCancellationRequested || Stopwatch.GetTimestamp() >= deadlineTimestamp)
                    break;

                var result = await ExecuteStepAsync(scenario.Target, step, token).ConfigureAwait(false);
                _aggregator.Record(in result);
                await _channel.Writer.WriteAsync(result, token).ConfigureAwait(false);
            }

            if (scenario.Load.ThinkTime > TimeSpan.Zero)
                await Task.Delay(scenario.Load.ThinkTime, token).ConfigureAwait(false);
        }
    }

    private async Task<RequestResult> ExecuteStepAsync(string target, RequestStep step, CancellationToken token)
    {
        var url = target.TrimEnd('/') + step.Path;
        var startNs = GetTimestampNs();

        _aggregator.IncrementActiveConnections();
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(step.Method), url);

            foreach (var (key, value) in step.Headers)
                request.Headers.TryAddWithoutValidation(key, value);

            if (step.Body is not null)
                request.Content = new StringContent(step.Body,
                    System.Text.Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request,
                HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);

            var bytes = 0;
            if (response.Content is not null)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(8192);
                try
                {
                    using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                    int read;
                    while ((read = await stream.ReadAsync(buffer, token).ConfigureAwait(false)) > 0)
                        bytes += read;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            var endNs = GetTimestampNs();
            var statusCode = (int)response.StatusCode;
            var isError = statusCode >= 400;
            return new RequestResult(startNs, endNs, statusCode, bytes, isError);
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            var endNs = GetTimestampNs();
            return RequestResult.Error(startNs, endNs);
        }
        finally
        {
            _aggregator.DecrementActiveConnections();
        }
    }

    private async Task ConsumeResultsAsync()
    {
        // TryComplete() is always called before this task is awaited, so CancellationToken.None is safe.
        await foreach (var _ in _channel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
        {
        }
    }

    private async Task EmitStatsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(StatsInterval, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var stats = _aggregator.Snapshot();
            StatsUpdated?.Invoke(stats);
        }
    }

    private static long GetTimestampNs() =>
        Stopwatch.GetTimestamp() * 1_000_000_000L / Stopwatch.Frequency;

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_consumerTask is not null)
            await _consumerTask.ConfigureAwait(false);
        _httpClient.Dispose();
        _cts?.Dispose();
    }
}
