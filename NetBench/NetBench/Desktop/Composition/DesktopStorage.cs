namespace NetBench.Desktop.Composition;

/// <summary>
/// Где desktop-версия хранит сценарии и настройки. По умолчанию — каталог данных пользователя
/// (в песочнице Mac App Store — контейнер приложения); генератор скриншотов передаёт временный каталог,
/// чтобы не читать и не трогать данные машины.
/// </summary>
public sealed record DesktopStorage(string? Root)
{
    public static DesktopStorage Default { get; } = new((string?)null);

    /// <summary>Каталог сценариев; <c>null</c> — по умолчанию (<see cref="Features.Scenarios.Data.JsonScenarioRepository"/>).</summary>
    public string? ScenariosDirectory => Root is null ? null : Path.Combine(Root, "scenarios");

    /// <summary>Файл настроек; <c>null</c> — по умолчанию (<see cref="Features.Settings.Data.JsonAppSettingsStore"/>).</summary>
    public string? SettingsPath => Root is null ? null : Path.Combine(Root, "settings.json");
}
