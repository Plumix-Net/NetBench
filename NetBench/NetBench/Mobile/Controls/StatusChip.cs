using Avalonia.Media;
using NetBench.Mobile.Theme;
using Plumix.Rendering;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

public enum StatusChipState
{
    Good,
    Bad,
}

/// <summary>Пилюля статуса последнего прогона (StatusChip из дизайн-проекта).</summary>
public sealed class StatusChip : StatelessWidget
{
    private readonly string _text;
    private readonly StatusChipState _state;

    public StatusChip(string text, StatusChipState state)
    {
        _text = text;
        _state = state;
    }

    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var (background, foreground) = _state == StatusChipState.Bad
            ? (palette.ErrorTint, palette.Error)
            : (palette.SuccessTint, palette.Success);

        return new Container(
            padding: new Avalonia.Thickness(9, 4),
            decoration: new BoxDecoration(
                Color: background,
                BorderRadius: BorderRadius.Circular(100)),
            child: new Text(
                _text,
                fontSize: 11.5,
                fontWeight: FontWeight.SemiBold,
                color: foreground,
                fontFamily: NetBenchFonts.Ui,
                maxLines: 1));
    }
}
