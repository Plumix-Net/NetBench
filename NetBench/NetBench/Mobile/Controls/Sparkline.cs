using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

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

        var points = new Point[_values.Count];
        for (var i = 0; i < _values.Count; i++)
        {
            var x = i / (double)(_values.Count - 1) * size.Width;
            var y = size.Height - inset - (_values[i] - min) / range * drawableHeight;
            points[i] = new Point(x, y);
        }

        var fillPoints = new Point[points.Length + 2];
        points.CopyTo(fillPoints, 0);
        fillPoints[^2] = new Point(size.Width, size.Height);
        fillPoints[^1] = new Point(0, size.Height);
        context.DrawPolygon(new SolidColorBrush(_fill), null, fillPoints);

        var pen = new Pen(
            new SolidColorBrush(_stroke),
            2,
            lineCap: PenLineCap.Round,
            lineJoin: PenLineJoin.Round);
        for (var i = 1; i < points.Length; i++)
            context.DrawLine(pen, points[i - 1], points[i]);
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not SparklinePainter old
        || !ReferenceEquals(old._values, _values)
        || old._stroke != _stroke
        || old._fill != _fill;
}
