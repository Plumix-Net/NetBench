using System.Text.Json;
using System.Text.Json.Serialization;
using NetBench.Features.Settings.Domain;

namespace NetBench.Features.Settings.Data;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext;

/// <summary>
/// Настройки в одном JSON-файле рядом со сценариями. Файл крошечный, поэтому читается
/// синхронно при создании: тема и язык нужны до первого кадра.
/// </summary>
public sealed class JsonAppSettingsStore : IAppSettingsStore
{
    private readonly string _path;

    public JsonAppSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetBench", "settings.json");

        Current = Load(_path);
    }

    public AppSettings Current { get; private set; }

    public void Save(AppSettings settings)
    {
        Current = settings;

        // Не записалось — настройка всё равно действует до перезапуска; падать из-за неё незачем.
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var temp = _path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings));
            File.Move(temp, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static AppSettings Load(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllText(path), AppSettingsJsonContext.Default.AppSettings)
                  ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }
}
