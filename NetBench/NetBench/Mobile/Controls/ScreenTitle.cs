using Avalonia.Media;
using NetBench.Mobile.Theme;
using Plumix.UI;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

/// <summary>
/// Заголовок экрана в верхней панели рядом с «назад». Помечен как header — TalkBack/VoiceOver
/// переходят по заголовкам; при крупном шрифте переносится на вторую строку, а не уезжает за край.
/// </summary>
public sealed class ScreenTitle(string text) : StatelessWidget
{
    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);

        return new Semantics(
            header: true,
            child: new Text(
                text,
                style: new TextStyle(
                    FontSize: 20,
                    FontWeight: FontWeight.ExtraBold,
                    Color: palette.TextHi,
                    FontFamily: NetBenchFonts.Ui),
                maxLines: 2,
                overflow: TextOverflow.Ellipsis));
    }
}
