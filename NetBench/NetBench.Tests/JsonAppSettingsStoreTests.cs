using NetBench.Features.Settings.Data;
using NetBench.Features.Settings.Domain;
using Xunit;

namespace NetBench.Tests;

public sealed class JsonAppSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "netbench-tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void MissingFileGivesDefaults()
    {
        var store = new JsonAppSettingsStore(SettingsPath);

        Assert.Equal(ThemePreference.System, store.Current.Theme);
        Assert.Null(store.Current.Culture);
        Assert.False(store.Current.OwnTargetsConfirmed);
    }

    [Fact]
    public void SavedSettingsSurviveRestart()
    {
        var store = new JsonAppSettingsStore(SettingsPath);
        store.Save(store.Current with { Theme = ThemePreference.Light, Culture = "ru-RU", OwnTargetsConfirmed = true });

        var restored = new JsonAppSettingsStore(SettingsPath).Current;

        Assert.Equal(ThemePreference.Light, restored.Theme);
        Assert.Equal("ru-RU", restored.Culture);
        Assert.True(restored.OwnTargetsConfirmed);
    }

    [Fact]
    public void CorruptedFileGivesDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, "[]");

        Assert.Equal(new AppSettings(), new JsonAppSettingsStore(SettingsPath).Current);
    }
}
