using NetBench.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetBench.Core.Storage;

public sealed class ScenarioRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private readonly string _directory;

    public ScenarioRepository(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NetBench", "scenarios");

        Directory.CreateDirectory(_directory);
    }

    public async Task<List<LoadScenario>> LoadAllAsync(CancellationToken ct = default)
    {
        var files = Directory.GetFiles(_directory, "*.json");
        var scenarios = new List<LoadScenario>(files.Length);

        foreach (var file in files)
        {
            var scenario = await LoadFileAsync(file, ct).ConfigureAwait(false);
            if (scenario is not null)
                scenarios.Add(scenario);
        }

        return scenarios;
    }

    public async Task<LoadScenario?> LoadAsync(Guid id, CancellationToken ct = default)
    {
        var path = GetPath(id);
        return File.Exists(path) ? await LoadFileAsync(path, ct).ConfigureAwait(false) : null;
    }

    public async Task SaveAsync(LoadScenario scenario, CancellationToken ct = default)
    {
        var path = GetPath(scenario.Id);
        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, scenario, JsonOptions, ct).ConfigureAwait(false);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var path = GetPath(id);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(Guid id) => Path.Combine(_directory, $"{id:N}.json");

    private static async Task<LoadScenario?> LoadFileAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<LoadScenario>(stream, JsonOptions, ct)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
