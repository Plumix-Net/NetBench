using NetBench.Mobile.Theme;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

/// <summary>Строка распределения задержек: метка, полоса и значение (LatencyBar compact из дизайна).</summary>
public sealed class LatencyBar : StatelessWidget
{
    private readonly string _label;
    private readonly double _fraction;
    private readonly string _display;

    /// <param name="fraction">Заполнение полосы, 0..1.</param>
    public LatencyBar(string label, double fraction, string display)
    {
        _label = label;
        _fraction = Math.Clamp(fraction, 0, 1);
        _display = display;
    }

    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);

        return new Row(
            spacing: 10,
            children:
            [
                new SizedBox(
                    width: 44,
                    child: new Text(
                        _label,
                        fontSize: 12,
                        color: palette.TextMid,
                        fontFamily: NetBenchFonts.Ui,
                        maxLines: 1)),
                new Expanded(
                    new ClipRRect(
                        BorderRadius.Circular(100),
                        new Container(
                            height: 8,
                            color: palette.Border,
                            child: new Align(
                                alignment: Alignment.CenterLeft,
                                child: new FractionallySizedBox(
                                    alignment: Alignment.CenterLeft,
                                    widthFactor: _fraction,
                                    heightFactor: 1,
                                    child: new Container(
                                        decoration: new BoxDecoration(
                                            Color: palette.Latency,
                                            BorderRadius: BorderRadius.Circular(100)))))))),
                new SizedBox(
                    width: 56,
                    child: new Text(
                        _display,
                        fontSize: 12.5,
                        color: palette.Text,
                        fontFamily: NetBenchFonts.Mono,
                        textAlign: TextAlign.Right,
                        maxLines: 1)),
            ]);
    }
}
