namespace NetBench.Features.Scenarios.Domain;

/// <summary>
/// Потолок нагрузки мобильной версии. Телефон — не стенд для стресс-теста, а сторы
/// не пропускают то, что выглядит как инструмент DoS: прогон с телефона — это проверка
/// своего API, а не попытка его уронить. Desktop лимитами не ограничен.
/// </summary>
public static class MobileLoadLimits
{
    public const int MaxUsers = 50;

    public static TimeSpan MaxDuration { get; } = TimeSpan.FromMinutes(5);

    /// <summary>Варианты числа пользователей для быстрого теста и редактора.</summary>
    public static IReadOnlyList<int> UserPresets { get; } = [1, 10, 25, MaxUsers];

    /// <summary>Варианты длительности для быстрого теста и редактора.</summary>
    public static IReadOnlyList<TimeSpan> DurationPresets { get; } =
    [
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1),
        MaxDuration,
    ];

    /// <summary>
    /// Нагрузка в пределах лимитов: сценарий мог прийти из старой версии или с desktop,
    /// поэтому лимиты применяются при каждом запуске, а не только в редакторе.
    /// </summary>
    public static LoadConfig Clamp(LoadConfig load)
    {
        var duration = load.Duration <= TimeSpan.Zero || load.Duration > MaxDuration
            ? MaxDuration
            : load.Duration;

        return new LoadConfig
        {
            ConcurrentUsers = Math.Clamp(load.ConcurrentUsers, 1, MaxUsers),
            Duration = duration,
            RampUp = load.RampUp > duration ? duration : load.RampUp,
            ThinkTime = load.ThinkTime,
        };
    }

    /// <summary>Копия сценария, которую можно отдать движку с телефона.</summary>
    public static LoadScenario Apply(LoadScenario scenario) => new()
    {
        Id = scenario.Id,
        Name = scenario.Name,
        Target = scenario.Target,
        IsQuickTest = scenario.IsQuickTest,
        Requests = scenario.Requests,
        Load = Clamp(scenario.Load),
    };
}
