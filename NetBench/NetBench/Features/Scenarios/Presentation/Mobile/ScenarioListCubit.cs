using Avalonia.Threading;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using Plumix.Bloc;
using NetBench.Localization;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>
/// Стейт-менеджер мобильного экрана списка сценариев.
/// Мобильный аналог desktop-ного <c>ScenarioListViewModel</c>: работает
/// с тем же доменным <see cref="IScenarioRepository"/>, но состояние
/// раздаёт по-Flutter'овски — через Cubit и иммутабельные снапшоты.
/// Дополнительно следит за <see cref="IReportStore"/>, чтобы показывать
/// чип последнего прогона на карточках.
/// </summary>
public sealed class ScenarioListCubit : Cubit<ScenarioListState>
{
    private readonly IScenarioRepository _repository;
    private readonly IReportStore _reports;

    public ScenarioListCubit(IScenarioRepository repository, IReportStore reports)
        : base(ScenarioListState.Initial)
    {
        _repository = repository;
        _reports = reports;
        _reports.Changed += OnReportsChanged;
    }

    public Task LoadAsync(CancellationToken ct = default)
    {
        Emit(State with { Status = ScenarioListStatus.Loading, Error = null });
        return RefreshAsync(ct);
    }

    /// <summary>
    /// Перечитывает список, не сбрасывая экран в состояние загрузки:
    /// pull-to-refresh рисует свой индикатор поверх уже показанных карточек.
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        try
        {
            var scenarios = await _repository.LoadAllAsync(ct);
            EmitSafe(new ScenarioListState(
                ScenarioListStatus.Ready,
                scenarios,
                LastRuns: SnapshotLastRuns(),
                Sessions: _reports.GetHistory()));
        }
        catch (Exception ex)
        {
            EmitSafe(State with { Status = ScenarioListStatus.Failure, Error = ex.Message });
        }
    }

    public async Task AddScenarioAsync(CancellationToken ct = default)
    {
        var scenario = new LoadScenario { Name = Strings.Instance.Root.Scenarios.New };
        scenario.Requests.Add(new RequestStep { Method = "GET", Path = "/" });

        await _repository.SaveAsync(scenario, ct);
        EmitSafe(State with
        {
            Status = ScenarioListStatus.Ready,
            Scenarios = [.. State.Scenarios, scenario],
        });
    }

    public async Task DeleteScenarioAsync(LoadScenario scenario, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(scenario.Id, ct);
        EmitSafe(State with
        {
            Scenarios = State.Scenarios.Where(s => s.Id != scenario.Id).ToList(),
        });
    }

    public override void Close()
    {
        _reports.Changed -= OnReportsChanged;
        base.Close();
    }

    // Отчёт сохраняется на UI-потоке (см. TestRunCubit), но не полагаемся на это
    private void OnReportsChanged() => Dispatcher.UIThread.Post(
        () => EmitSafe(State with { LastRuns = SnapshotLastRuns(), Sessions = _reports.GetHistory() }));

    private Dictionary<Guid, TestRunReport> SnapshotLastRuns() =>
        _reports.GetAll().ToDictionary(report => report.Scenario.Id);

    // После await кубит мог быть закрыт (виджет размонтирован) — молча выходим.
    private void EmitSafe(ScenarioListState state)
    {
        if (!IsClosed)
            Emit(state);
    }
}
