namespace NetBench.Features.Settings.Domain;

/// <summary>Выбор оформления: <see cref="System"/> — следовать теме устройства.</summary>
public enum ThemePreference
{
    System,
    Dark,
    Light,
}

/// <summary>Пользовательские настройки приложения, переживающие перезапуск.</summary>
public sealed record AppSettings
{
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    /// <summary>Имя культуры интерфейса («ru-RU»); <c>null</c> — язык устройства.</summary>
    public string? Culture { get; init; }
}

public interface IAppSettingsStore
{
    /// <summary>Текущие настройки: прочитанные при старте или последние сохранённые.</summary>
    AppSettings Current { get; }

    void Save(AppSettings settings);
}
