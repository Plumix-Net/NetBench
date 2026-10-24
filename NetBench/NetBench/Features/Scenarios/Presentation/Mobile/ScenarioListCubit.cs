using Avalonia.Threading;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.TestRun.Domain;
using Plumix.Bloc;

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

    /// <summary>
    /// Создаёт или обновляет сценарий (по <see cref="LoadScenario.Id"/>): сначала запись
    /// на диск, затем состояние — карточка появляется, только если сценарий сохранён.
    /// </summary>
    /// <returns><c>false</c>, если записать сценарий не удалось.</returns>
    public async Task<bool> SaveScenarioAsync(LoadScenario scenario, CancellationToken ct = default)
    {
        try
        {
            await _repository.SaveAsync(scenario, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return false;
        }

        var scenarios = State.Scenarios.ToList();
        var index = scenarios.FindIndex(s => s.Id == scenario.Id);
        if (index >= 0)
            scenarios[index] = scenario;
        else
            scenarios.Add(scenario);

        EmitSafe(State with { Status = ScenarioListStatus.Ready, Scenarios = scenarios });
        return true;
    }

    /// <summary>
    /// Убирает карточку из состояния сразу, до записи на диск: Dismissible уже отыграл
    /// анимацию и ждёт, что его выкинут из дерева в этом же кадре. Если удалить файл
    /// не вышло — перечитываем репозиторий, и карточка возвращается.
    /// </summary>
    /// <returns><c>false</c>, если сценарий удалить не удалось.</returns>
    public async Task<bool> DeleteScenarioAsync(LoadScenario scenario, CancellationToken ct = default)
    {
        EmitSafe(State with
        {
            Scenarios = State.Scenarios.Where(s => s.Id != scenario.Id).ToList(),
        });

        try
        {
            await _repository.DeleteAsync(scenario.Id, ct);
            return true;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            await RefreshAsync(CancellationToken.None);
            return false;
        }
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
