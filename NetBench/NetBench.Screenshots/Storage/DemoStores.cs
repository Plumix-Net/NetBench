using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Settings.Domain;
using NetBench.Features.TestRun.Domain;
using NetBench.Screenshots.Storage;

// Те же имена и пространства имён, что у JSON-хранилищ приложения (их исходники исключены в csproj):
// MobileShell создаёт их конструкторами по умолчанию и получает данные из DemoStorage.

namespace NetBench.Features.Scenarios.Data
{
    public sealed class JsonScenarioRepository : IScenarioRepository
    {
        public Task<List<LoadScenario>> LoadAllAsync(CancellationToken ct = default) =>
            Task.FromResult(DemoStorage.Scenarios.ToList());

        public Task<LoadScenario?> LoadAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(DemoStorage.Scenarios.FirstOrDefault(scenario => scenario.Id == id));

        public Task SaveAsync(LoadScenario scenario, CancellationToken ct = default)
        {
            DemoStorage.Scenarios.RemoveAll(existing => existing.Id == scenario.Id);
            DemoStorage.Scenarios.Add(scenario);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            DemoStorage.Scenarios.RemoveAll(scenario => scenario.Id == id);
            return Task.CompletedTask;
        }
    }
}

namespace NetBench.Features.TestRun.Data
{
    public sealed class JsonReportStore : IReportStore
    {
        public event Action? Changed
        {
            add => DemoStorage.Reports.Changed += value;
            remove => DemoStorage.Reports.Changed -= value;
        }

        public void Save(TestRunReport report) => DemoStorage.Reports.Save(report);

        public TestRunReport? GetLatest(Guid scenarioId) => DemoStorage.Reports.GetLatest(scenarioId);

        public IReadOnlyList<TestRunReport> GetAll() => DemoStorage.Reports.GetAll();

        public IReadOnlyList<TestRunReport> GetHistory() => DemoStorage.Reports.GetHistory();
    }
}

namespace NetBench.Features.Settings.Data
{
    public sealed class JsonAppSettingsStore : IAppSettingsStore
    {
        public AppSettings Current => DemoStorage.Settings;

        public void Save(AppSettings settings) => DemoStorage.Settings = settings;
    }
}
