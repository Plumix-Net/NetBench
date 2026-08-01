using System.Diagnostics;

namespace NetBench.Features.TestRun.Domain;

/// <summary>
/// Монотонные наносекундные метки времени для горячего пути движка.
/// Наивное <c>GetTimestamp() * 1e9 / Frequency</c> переполняет long на платформах
/// с Frequency = 1 ГГц (Linux/Android): уже при аптайме ~9.2 с произведение
/// выходит за long.MaxValue и все метки превращаются в мусор.
/// </summary>
public static class MonotonicClock
{
    /// <summary>Текущая метка в наносекундах — без переполнения при любом аптайме.</summary>
    public static long NowNanoseconds() => ToNanoseconds(Stopwatch.GetTimestamp());

    public static long ToNanoseconds(long timestamp)
    {
        var frequency = Stopwatch.Frequency;
        var seconds = timestamp / frequency;
        var remainderTicks = timestamp % frequency;
        return seconds * 1_000_000_000L + remainderTicks * 1_000_000_000L / frequency;
    }
}
