using NetBench.Features.Scenarios.Domain;
using Plumix.Bloc;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>
/// Стейт-менеджер мобильного экрана списка сценариев.
/// Мобильный аналог desktop-ного <c>ScenarioListViewModel</c>: работает
/// с тем же доменным <see cref="IScenarioRepository"/>, но состояние
/// раздаёт по-Flutter'овски — через Cubit и иммутабельные снапшоты.
/// </summary>
public sealed class ScenarioListCubit : Cubit<ScenarioListState>
{
    private readonly IScenarioRepository _repository;

    public ScenarioListCubit(IScenarioRepository repository)
        : base(ScenarioListState.Initial)
    {
        _repository = repository;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Emit(State with { Status = ScenarioListStatus.Loading, Error = null });

        try
        {
            var scenarios = await _repository.LoadAllAsync(ct);
            EmitSafe(new ScenarioListState(ScenarioListStatus.Ready, scenarios));
        }
        catch (Exception ex)
        {
            EmitSafe(State with { Status = ScenarioListStatus.Failure, Error = ex.Message });
        }
    }

    public async Task AddScenarioAsync(CancellationToken ct = default)
    {
        var scenario = new LoadScenario { Name = "Новый сценарий" };
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

    // После await кубит мог быть закрыт (виджет размонтирован) — молча выходим.
    private void EmitSafe(ScenarioListState state)
    {
        if (!IsClosed)
            Emit(state);
    }
}
