using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace NetBench.Mobile.Controls;

/// <summary>Спарклайн живой метрики (LineChart из дизайн-проекта): линия + мягкая заливка.</summary>
public sealed class Sparkline : StatelessWidget
{
    private readonly IReadOnlyList<double> _values;
    private readonly Color _stroke;
    private readonly Color _fill;
    private readonly double _height;

    public Sparkline(IReadOnlyList<double> values, Color stroke, Color fill, double height = 56)
    {
        _values = values;
        _stroke = stroke;
        _fill = fill;
        _height = height;
    }

    public override Widget Build(BuildContext context) =>
        new CustomPaint(
            painter: new SparklinePainter(_values, _stroke, _fill),
            size: new Size(double.PositiveInfinity, _height));
}

internal sealed class SparklinePainter : CustomPainter
{
    private readonly IReadOnlyList<double> _values;
    private readonly Color _stroke;
    private readonly Color _fill;

    public SparklinePainter(IReadOnlyList<double> values, Color stroke, Color fill)
    {
        _values = values;
        _stroke = stroke;
        _fill = fill;
    }

    public override void Paint(PaintingContext context, Size size)
    {
        if (_values.Count < 2 || size.Width <= 0 || size.Height <= 0)
            return;

        var min = double.MaxValue;
        var max = double.MinValue;
        foreach (var value in _values)
        {
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }

        var range = max - min;
        if (range <= 0)
            range = 1;

        // 2px вертикального запаса, чтобы линия не обрезалась на экстремумах
        const double inset = 2;
        var drawableHeight = Math.Max(1, size.Height - inset * 2);

        var line = new Path();
        var area = new Path();
        area.MoveTo(0, size.Height);
        for (var i = 0; i < _values.Count; i++)
        {
            var x = i / (double)(_values.Count - 1) * size.Width;
            var y = size.Height - inset - (_values[i] - min) / range * drawableHeight;
            if (i == 0)
                line.MoveTo(x, y);
            else
                line.LineTo(x, y);
            area.LineTo(x, y);
        }

        area.LineTo(size.Width, size.Height);
        area.Close();
        context.Canvas.DrawPath(area, new Paint { Color = _fill });
        context.Canvas.DrawPath(line, new Paint
        {
            Color = _stroke,
            Style = PaintingStyle.Stroke,
            StrokeWidth = 2,
            StrokeCap = StrokeCap.Round,
            StrokeJoin = StrokeJoin.Round,
        });
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not SparklinePainter old
        || !ReferenceEquals(old._values, _values)
        || old._stroke != _stroke
        || old._fill != _fill;
}
