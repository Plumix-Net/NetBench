namespace NetBench.Features.TestRun.Domain;

public readonly struct RequestResult
{
    public readonly long StartTimestampNs;
    public readonly long EndTimestampNs;
    public readonly int StatusCode;
    public readonly int BytesReceived;
    public readonly bool IsError;

    public long LatencyNs => EndTimestampNs - StartTimestampNs;
    public double LatencyMs => LatencyNs / 1_000_000.0;

    public RequestResult(long startNs, long endNs, int statusCode, int bytesReceived, bool isError)
    {
        StartTimestampNs = startNs;
        EndTimestampNs = endNs;
        StatusCode = statusCode;
        BytesReceived = bytesReceived;
        IsError = isError;
    }

    public static RequestResult Error(long startNs, long endNs) =>
        new(startNs, endNs, 0, 0, true);
}
