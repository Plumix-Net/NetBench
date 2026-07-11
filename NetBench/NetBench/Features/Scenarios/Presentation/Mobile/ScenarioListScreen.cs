using NetBench.Features.Scenarios.Domain;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Widgets;

namespace NetBench.Features.Scenarios.Presentation.Mobile;

/// <summary>Мобильный экран списка сценариев на Plumix-виджетах.</summary>
public sealed class ScenarioListScreen : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Scaffold(
            appBar: new AppBar(titleText: "Сценарии"),
            body: new BlocBuilder<ScenarioListCubit, ScenarioListState>(
                builder: BuildBody),
            floatingActionButton: new FloatingActionButton(
                child: new Icon(Icons.Add),
                onPressed: () => _ = context.Read<ScenarioListCubit>().AddScenarioAsync(),
                tooltip: "Новый сценарий"));
    }

    private static Widget BuildBody(BuildContext context, ScenarioListState state)
    {
        return state.Status switch
        {
            ScenarioListStatus.Loading => new Center(child: new CircularProgressIndicator()),
            ScenarioListStatus.Failure => new Center(
                child: new Text($"Не удалось загрузить сценарии: {state.Error}")),
            _ when state.Scenarios.Count == 0 => new Center(
                child: new Text("Нет сценариев — создайте первый кнопкой «+»")),
            _ => ListView.Builder(
                itemCount: state.Scenarios.Count,
                itemBuilder: (itemContext, index) =>
                    BuildTile(itemContext, state.Scenarios[index])),
        };
    }

    private static ListTile BuildTile(BuildContext context, LoadScenario scenario)
    {
        return new ListTile(
            title: new Text(scenario.Name),
            subtitle: new Text(Subtitle(scenario)),
            trailing: new IconButton(
                icon: new Icon(Icons.Clear),
                onPressed: () => _ = context.Read<ScenarioListCubit>().DeleteScenarioAsync(scenario)));
    }

    private static string Subtitle(LoadScenario scenario)
    {
        var target = string.IsNullOrWhiteSpace(scenario.Target) ? "цель не задана" : scenario.Target;
        return $"{target} · запросов: {scenario.Requests.Count}";
    }
}
