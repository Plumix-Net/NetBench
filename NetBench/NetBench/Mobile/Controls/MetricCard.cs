using Avalonia.Media;
using NetBench.Mobile.Theme;
using Plumix.Rendering;
using Plumix.Widgets;

namespace NetBench.Mobile.Controls;

/// <summary>Семантический тон метрики — мобильный аналог MetricTone из Desktop/Controls.</summary>
public enum MetricTone
{
    Neutral,
    Rps,
    Latency,
    Error,
    Success,
}

/// <summary>Компактная карточка метрики (MetricCard compact из дизайн-проекта).</summary>
public sealed class MetricCard : StatelessWidget
{
    private readonly string _label;
    private readonly string _value;
    private readonly string? _unit;
    private readonly MetricTone _tone;

    public MetricCard(string label, string value, string? unit = null, MetricTone tone = MetricTone.Neutral)
    {
        _label = label;
        _value = value;
        _unit = unit;
        _tone = tone;
    }

    public override Widget Build(BuildContext context)
    {
        var palette = NetBenchTheme.PaletteOf(context);
        var toneColor = _tone switch
        {
            MetricTone.Rps => palette.Rps,
            MetricTone.Latency => palette.Latency,
            MetricTone.Error => palette.Error,
            MetricTone.Success => palette.Success,
            _ => palette.TextHi,
        };

        var valueRow = new List<Widget>
        {
            new Text(
                _value,
                style: new TextStyle(
                    FontSize: 24,
                    FontWeight: FontWeight.ExtraBold,
                    Color: toneColor,
                    FontFamily: NetBenchFonts.Mono),
                maxLines: 1),
        };
        if (!string.IsNullOrEmpty(_unit))
        {
            valueRow.Add(new Padding(
                new Avalonia.Thickness(1, 0, 0, 2),
                new Text(
                    _unit,
                    style: new TextStyle(
                        FontSize: 15,
                        FontWeight: FontWeight.Bold,
                        Color: toneColor,
                        FontFamily: NetBenchFonts.Mono))));
        }

        // Один узел семантики на карточку: «P95 latency, 5 ms», а не три отдельные строки
        return new MergeSemantics(new Container(
            padding: new Avalonia.Thickness(14),
            decoration: new BoxDecoration(
                Color: palette.BgCard,
                BorderRadius: BorderRadius.Circular(10)),
            child: new Column(
                crossAxisAlignment: CrossAxisAlignment.Start,
                mainAxisSize: MainAxisSize.Min,
                children:
                [
                    new Text(
                        _label.ToUpperInvariant(),
                        style: new TextStyle(
                            FontSize: 11,
                            FontWeight: FontWeight.Bold,
                            Color: palette.TextMid,
                            LetterSpacing: 0.5,
                            FontFamily: NetBenchFonts.Ui),
                        // Крупный шрифт: подпись переносится, а значение ужимается под карточку —
                        // половина экрана не вмещает «49 187» в 48 pt
                        maxLines: 2),
                    new SizedBox(height: 6),
                    new FittedBox(
                        fit: BoxFit.ScaleDown,
                        alignment: Alignment.CenterLeft,
                        child: new Row(
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.End,
                            children: valueRow)),
                ])));
    }
}
