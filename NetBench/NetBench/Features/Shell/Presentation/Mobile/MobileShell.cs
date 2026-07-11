using NetBench.Features.Scenarios.Data;
using NetBench.Features.Scenarios.Domain;
using NetBench.Features.Scenarios.Presentation.Mobile;
using Plumix.Bloc;
using Plumix.Material;
using Plumix.Widgets;

namespace NetBench.Features.Shell.Presentation.Mobile;

/// <summary>
/// Корневой виджет мобильного приложения: тема, провайдеры domain/data-слоя
/// и стартовый экран. Мобильный composition root — аналог Pure.DI-композиции
/// desktop-версии, но в стиле Flutter (Repository/Bloc-провайдеры).
/// </summary>
public sealed class MobileShell : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        return new Theme(
            data: ThemeData.Light,
            child: new ScaffoldMessenger(
                child: new RepositoryProvider<IScenarioRepository>(
                    value: new JsonScenarioRepository(),
                    child: new BlocProvider<ScenarioListCubit>(
                        create: static providerContext =>
                        {
                            var cubit = new ScenarioListCubit(
                                RepositoryProvider<IScenarioRepository>.ReadOf(providerContext));
                            _ = cubit.LoadAsync();
                            return cubit;
                        },
                        child: new ScenarioListScreen()))));
    }
}
