using NetBench.Mobile.Theme;
using Plumix.Foundation;
using Plumix.Widgets;

namespace NetBench.Mobile.Navigation;

/// <summary>
/// Роут, содержимое которого пересоздаётся при переключении темы.
/// </summary>
/// <remarks>
/// Plumix запекает кисть в TextLayout, а сеттер Foreground делает только MarkNeedsPaint —
/// при смене одного лишь цвета текст остаётся старым. Обход — пересобрать поддерево
/// экрана по ключу от темы. Ключ живёт <b>внутри</b> роута, а не над Navigator: стек
/// навигации переживает переключение, поэтому тумблер темы доступен с любого экрана.
/// </remarks>
public static class ThemedPageRoute
{
    public static BuilderPageRoute Of(Func<BuildContext, Widget> builder, RouteSettings? settings = null) =>
        new(
            context => new KeyedSubtree(
                key: new ValueKey<bool>(NetBenchTheme.PaletteOf(context).IsDark),
                child: builder(context)),
            settings: settings);
}
