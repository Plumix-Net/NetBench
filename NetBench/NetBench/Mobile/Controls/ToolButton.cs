using NetBench.Mobile.Theme;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

/// <summary>
/// Квадратная кнопка-иконка на подложке карточки («назад», настройки, тема).
/// Подложка — фон самого IconButton, а не Material вокруг него: M3 добивает зону касания
/// до 48 dp, и обёртка закрасила бы всю эту зону вместо видимых <paramref name="size"/> dp.
/// </summary>
public sealed class ToolButton(Widget icon, Action onPressed, string tooltip, double size = 36, double radius = 9)
    : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);

        return new IconButton(
            icon: icon,
            onPressed: onPressed,
            tooltip: tooltip,
            style: IconButton.StyleFrom(
                backgroundColor: palette.BgCard,
                shape: BorderRadius.Circular(radius),
                fixedSize: new Avalonia.Size(size, size),
                minimumSize: new Avalonia.Size(size, size),
                padding: default(Avalonia.Thickness)));
    }
}
