namespace NetBench.Features.Scenarios.Domain;

/// <summary>
/// Хранилище сценариев нагрузки. Контракт domain-слоя фичи «Сценарии»,
/// реализуется data-слоем и переиспользуется всеми платформами.
/// </summary>
public interface IScenarioRepository
{
    Task<List<LoadScenario>> LoadAllAsync(CancellationToken ct = default);
    Task<LoadScenario?> LoadAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(LoadScenario scenario, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
