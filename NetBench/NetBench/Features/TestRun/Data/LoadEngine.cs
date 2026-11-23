using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Channels;

namespace NetBench.Features.TestRun.Data;

public sealed class LoadEngine : IAsyncDisposable
{
    private static readonly TimeSpan _statsInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _httpClient;
    private readonly Channel<RequestResult> _channel;
    private StatisticsAggregator _aggregator;

    private Task? _consumerTask;
    private CancellationTokenSource? _cts;

    public event Action<TestRunStats>? StatsUpdated;
    public event Action<TestRunReport>? RunCompleted;

    public LoadEngine()
    {
        // SocketsHttpHandler явно, а не платформенный по умолчанию (на Android — AndroidMessageHandler
        // поверх Java HttpURLConnection, на iOS — NSUrlSession): движок ведёт себя одинаково на всех
        // платформах, полностью асинхронен и сам держит пул соединений. На эмуляторе Android
        // против одной и той же цели это дало вдвое больше RPS (6 → 11 при 10 пользователях).
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            // Виртуальные пользователи независимы: общий контейнер cookie склеил бы их сессии
            // (и сериализовал бы запросы на его lock).
            UseCookies = false,
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };
        _channel = Channel.CreateBounded<RequestResult>(new BoundedChannelOptions(65536)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });
        _aggregator = new StatisticsAggregator();
    }

    public async Task RunAsync(LoadScenario scenario, CancellationToken externalToken = default)
    {
        // Без абсолютного http(s)-адреса каждый запрос падает, не дойдя до сети, — за секунду
        // набегают миллионы «ошибок» и бессмысленный RPS. Такой прогон не запускаем вовсе.
        if (!TargetUrl.IsValid(scenario.Target))
            throw new ArgumentException($"Target is not an absolute http(s) URL: '{scenario.Target}'", nameof(scenario));

        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        var token = _cts.Token;
        var startedAt = DateTime.UtcNow;
        // Время прогона — с его старта, а не с создания движка.
        _aggregator = new StatisticsAggregator();

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

            await _cts.CancelAsync().ConfigureAwait(false);
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

    private static readonly RequestStep _defaultStep = new() { Method = "GET", Path = "/" };

    private async Task WorkerAsync(LoadScenario scenario, long deadlineTimestamp, int workerIndex, CancellationToken token)
    {
        // Сценарий без шагов — валидный случай: просто бьём GET'ом в сам Target.
        IReadOnlyList<RequestStep> steps = scenario.Requests.Count > 0
            ? scenario.Requests
            : [_defaultStep];

        _aggregator.IncrementActiveUsers();
        try
        {
            while (!token.IsCancellationRequested &&
                   Stopwatch.GetTimestamp() < deadlineTimestamp)
            {
                foreach (var step in steps)
                {
                    if (token.IsCancellationRequested || Stopwatch.GetTimestamp() >= deadlineTimestamp)
                        break;

                    var result = await ExecuteStepAsync(scenario.Target, step, token).ConfigureAwait(false);
                    _aggregator.Record(in result);
                    await _channel.Writer.WriteAsync(result, token).ConfigureAwait(false);

                    // Битый запрос не исправится от повторов: останавливаем прогон, отчёт покажет причину.
                    if (result.ErrorKind == RequestErrorKind.InvalidRequest)
                    {
                        await _cts!.CancelAsync().ConfigureAwait(false);
                        return;
                    }
                }

                if (scenario.Load.ThinkTime > TimeSpan.Zero)
                    await Task.Delay(scenario.Load.ThinkTime, token).ConfigureAwait(false);
            }
        }
        finally
        {
            _aggregator.DecrementActiveUsers();
        }
    }

    private async Task<RequestResult> ExecuteStepAsync(string target, RequestStep step, CancellationToken token)
    {
        // Path "/" или пустой — значит бить в Target как есть (пользователь мог
        // указать в Target полный URL с путём); иначе приклеиваем path к базе.
        var url = step.Path is "" or "/"
            ? target
            : target.TrimEnd('/') + step.Path;
        var startNs = MonotonicClock.NowNanoseconds();

        _aggregator.IncrementActiveConnections();
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(step.Method), url);

            foreach (var (key, value) in step.Headers)
                request.Headers.TryAddWithoutValidation(key, value);

            if (step.Body is not null)
                request.Content = new StringContent(step.Body,
                    System.Text.Encoding.UTF8, "application/json");

            // ResponseHeadersRead + чтение потока в буфер из пула: тело считается, но не копится
            // целиком в памяти (ResponseContentRead буферизовал бы каждый ответ).
            using var response = await _httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

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

            var endNs = MonotonicClock.NowNanoseconds();
            var statusCode = (int)response.StatusCode;
            var isError = statusCode >= 400;
            return new RequestResult(startNs, endNs, statusCode, bytes, isError);
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
            // Остановка прогона: обрыв запроса — не ошибка цели. Наружу — всегда отмена,
            // какое бы исключение ни бросил обрываемый запрос.
            throw new OperationCanceledException(token);
        }
        catch (Exception ex)
        {
            var endNs = MonotonicClock.NowNanoseconds();
            var kind = Classify(ex);
            if (_aggregator.NeedsErrorMessage(kind))
                _aggregator.RecordErrorMessage(kind, Describe(ex));
            return RequestResult.Error(startNs, endNs, kind);
        }
        finally
        {
            _aggregator.DecrementActiveConnections();
        }
    }

    /// <summary>Причина сбоя запроса — для разбивки ошибок в отчёте.</summary>
    internal static RequestErrorKind Classify(Exception ex)
    {
        switch (ex)
        {
            // Тайм-аут HttpClient приходит как отмена, но наш токен не отменён (это отфильтровано выше).
            case TaskCanceledException or OperationCanceledException or TimeoutException:
                return RequestErrorKind.Timeout;

            case HttpRequestException http:
                switch (http.HttpRequestError)
                {
                    case HttpRequestError.NameResolutionError:
                        return RequestErrorKind.Dns;
                    case HttpRequestError.SecureConnectionError:
                        return RequestErrorKind.Tls;
                    case HttpRequestError.HttpProtocolError or HttpRequestError.InvalidResponse
                        or HttpRequestError.ResponseEnded or HttpRequestError.VersionNegotiationError:
                        return RequestErrorKind.Protocol;
                    case HttpRequestError.ConnectionError:
                        return FindSocketError(ex) switch
                        {
                            SocketError.ConnectionRefused => RequestErrorKind.ConnectionRefused,
                            SocketError.TimedOut => RequestErrorKind.Timeout,
                            SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain => RequestErrorKind.Dns,
                            _ => RequestErrorKind.Connection,
                        };
                }

                if (ex.InnerException is AuthenticationException)
                    return RequestErrorKind.Tls;
                return FindSocketError(ex) is { } socketError
                    ? socketError == SocketError.ConnectionRefused ? RequestErrorKind.ConnectionRefused : RequestErrorKind.Connection
                    : RequestErrorKind.Other;

            case IOException or SocketException:
                return RequestErrorKind.Connection;

            // Запрос не собрать: относительный/битый URI, недопустимый метод, схема не http(s).
            case InvalidOperationException or UriFormatException or FormatException
                or NotSupportedException or ArgumentException:
                return RequestErrorKind.InvalidRequest;

            default:
                return RequestErrorKind.Other;
        }
    }

    private static SocketError? FindSocketError(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
        {
            if (ex is SocketException socket)
                return socket.SocketErrorCode;
        }

        return null;
    }

    /// <summary>Самое конкретное сообщение из цепочки исключений — его и покажем пользователю.</summary>
    private static string Describe(Exception ex)
    {
        var innermost = ex;
        while (innermost.InnerException is not null)
            innermost = innermost.InnerException;

        return innermost == ex || string.IsNullOrWhiteSpace(innermost.Message)
            ? ex.Message
            : $"{ex.Message} ({innermost.Message})";
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
                await Task.Delay(_statsInterval, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var stats = _aggregator.Snapshot();
            StatsUpdated?.Invoke(stats);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);
        if (_consumerTask is not null)
            await _consumerTask.ConfigureAwait(false);
        _httpClient.Dispose();
        _cts?.Dispose();
    }
}
