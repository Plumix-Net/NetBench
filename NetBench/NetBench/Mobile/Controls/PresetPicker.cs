using Avalonia.Media;
using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

/// <summary>
/// Ряд взаимоисключающих пресетов (число пользователей, длительность): равные ячейки,
/// выбранная подсвечена акцентом RPS. Пресеты вместо свободного ввода держат нагрузку
/// в пределах MobileLoadLimits и не требуют клавиатуры.
/// </summary>
public sealed class PresetPicker<T>(
    IReadOnlyList<T> values,
    T selected,
    Func<T, string> label,
    Action<T> onSelected) : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var comparer = EqualityComparer<T>.Default;

        return new Row(
            spacing: 8,
            children:
            [
                .. values.Select(Widget (value) => new Expanded(
                    BuildCell(palette, label(value), comparer.Equals(value, selected), () => onSelected(value)))),
            ]);
    }

    private static Semantics BuildCell(NetBenchPalette palette, string text, bool isSelected, Action onTap)
    {
        var radius = BorderRadius.Circular(10);

        // inMutuallyExclusiveGroup + selected: TalkBack/VoiceOver читают ряд как группу переключателей.
        return new Semantics(
            button: true,
            selected: isSelected,
            inMutuallyExclusiveGroup: true,
            child: new Material(
                color: isSelected ? palette.RpsTint : palette.BgCard,
                borderRadius: radius,
                child: new InkWell(
                    onTap: isSelected ? null : onTap,
                    borderRadius: radius,
                    child: new Container(
                        // 48 dp — минимальная цель касания из гайдлайнов Android/iOS
                        constraints: new BoxConstraints(MinHeight: 48),
                        alignment: Alignment.Center,
                        padding: new Avalonia.Thickness(4, 8),
                        decoration: new BoxDecoration(
                            BorderRadius: radius,
                            Border: Border.All(
                                isSelected ? palette.Rps : palette.Border,
                                width: isSelected ? 1.5 : 1)),
                        child: new Text(
                            text,
                            style: new TextStyle(
                                FontSize: 14,
                                FontWeight: isSelected ? FontWeight.Bold : FontWeight.Medium,
                                Color: isSelected ? palette.Rps : palette.Text,
                                FontFamily: NetBenchFonts.Mono),
                            textAlign: TextAlign.Center,
                            maxLines: 1)))));
    }
}
