using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Path = Plumix.UI.Path;

namespace NetBench.Mobile.Controls;

/// <summary>Векторные пиктограммы из мобильного макета, которых нет в Material-наборе Plumix.</summary>
public enum GlyphKind
{
    Play,
    Stop,
    Share,
    Sun,
    Moon,
    Plus,
    ChevronRight,
    Gear,
    Check,
}

/// <summary>Рисует иконку дизайн-макета указанного цвета и размера.</summary>
public sealed class IconGlyph : StatelessWidget
{
    private readonly GlyphKind _kind;
    private readonly Color _color;
    private readonly double _size;

    public IconGlyph(GlyphKind kind, Color color, double size)
    {
        _kind = kind;
        _color = color;
        _size = size;
    }

    public override Widget Build(BuildContext context) =>
        new SizedBox(
            width: _size,
            height: _size,
            child: new CustomPaint(
                painter: new GlyphPainter(_kind, _color),
                size: new Size(_size, _size)));
}

internal sealed class GlyphPainter : CustomPainter
{
    private readonly GlyphKind _kind;
    private readonly Color _color;

    public GlyphPainter(GlyphKind kind, Color color)
    {
        _kind = kind;
        _color = color;
    }

    public override void Paint(PaintingContext context, Size size)
    {
        var canvas = context.Canvas;
        var fill = new Paint { Color = _color };
        var w = size.Width;
        var h = size.Height;

        switch (_kind)
        {
            case GlyphKind.Play:
                canvas.DrawPath(Polyline(close: true, new Offset(0, 0), new Offset(w, h / 2), new Offset(0, h)), fill);
                break;

            case GlyphKind.Stop:
                canvas.DrawRect(Rect.FromLTWH(0, 0, w, h), fill);
                break;

            case GlyphKind.Plus:
            {
                var pen = Stroke(w * 2 / 14);
                canvas.DrawLine(new Offset(w / 2, 0), new Offset(w / 2, h), pen);
                canvas.DrawLine(new Offset(0, h / 2), new Offset(w, h / 2), pen);
                break;
            }

            case GlyphKind.Share:
            {
                // Стрелка вверх над лотком — как в макете (viewBox 14x14)
                var pen = Stroke(w * 1.6 / 14);
                var u = w / 14;
                canvas.DrawLine(new Offset(7 * u, 0.5 * u), new Offset(7 * u, 9 * u), pen);
                canvas.DrawLine(new Offset(3 * u, 4 * u), new Offset(7 * u, 0.5 * u), pen);
                canvas.DrawLine(new Offset(11 * u, 4 * u), new Offset(7 * u, 0.5 * u), pen);
                canvas.DrawPath(
                    Polyline(
                        close: false,
                        new Offset(1 * u, 9 * u),
                        new Offset(1 * u, 13 * u),
                        new Offset(13 * u, 13 * u),
                        new Offset(13 * u, 9 * u)),
                    pen);
                break;
            }

            case GlyphKind.Sun:
            {
                var center = new Offset(w / 2, h / 2);
                canvas.DrawCircle(center, w * 3.1 / 16, fill);
                var pen = Stroke(w * 1.4 / 16);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var (sin, cos) = Math.SinCos(angle);
                    var inner = new Offset(center.Dx + cos * w * 0.31, center.Dy + sin * h * 0.31);
                    var outer = new Offset(center.Dx + cos * w * 0.46, center.Dy + sin * h * 0.46);
                    canvas.DrawLine(inner, outer, pen);
                }

                break;
            }

            case GlyphKind.ChevronRight:
                canvas.DrawPath(
                    Polyline(
                        close: false,
                        new Offset(w * 0.35, h * 0.15),
                        new Offset(w * 0.7, h * 0.5),
                        new Offset(w * 0.35, h * 0.85)),
                    Stroke(w * 1.6 / 12));
                break;

            case GlyphKind.Check:
                canvas.DrawPath(
                    Polyline(
                        close: false,
                        new Offset(w * 0.16, h * 0.53),
                        new Offset(w * 0.40, h * 0.78),
                        new Offset(w * 0.84, h * 0.24)),
                    Stroke(w * 2.0 / 14));
                break;

            case GlyphKind.Gear:
            {
                // Шестерёнка: кольцо из 8 зубцов-лучей плюс обод и втулка —
                // рисуем штрихами, чтобы толщина совпадала с остальными глифами.
                var center = new Offset(w / 2, h / 2);
                var pen = Stroke(w * 1.6 / 16);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var (sin, cos) = Math.SinCos(angle);
                    var inner = new Offset(center.Dx + cos * w * 0.33, center.Dy + sin * h * 0.33);
                    var outer = new Offset(center.Dx + cos * w * 0.47, center.Dy + sin * h * 0.47);
                    canvas.DrawLine(inner, outer, pen);
                }

                canvas.DrawOval(Rect.FromLTWH(w * 0.20, h * 0.20, w * 0.60, h * 0.60), pen);
                canvas.DrawCircle(center, w * 0.11, fill);
                break;
            }

            case GlyphKind.Moon:
            {
                // Полумесяц: из большого круга вырезан смещённый круг
                var full = new Path();
                full.AddOval(Rect.FromLTWH(0, 0, w, h));
                var bite = new Path();
                bite.AddOval(Rect.FromLTWH(w * 0.25, -h * 0.25, w, h));
                canvas.DrawPath(Path.Combine(PathOperation.Difference, full, bite), fill);
                break;
            }
        }
    }

    private Paint Stroke(double thickness) => new()
    {
        Color = _color,
        Style = PaintingStyle.Stroke,
        StrokeWidth = thickness,
        StrokeCap = StrokeCap.Round,
        StrokeJoin = StrokeJoin.Round,
    };

    private static Path Polyline(bool close, params Offset[] points)
    {
        var path = new Path();
        path.MoveTo(points[0].Dx, points[0].Dy);
        foreach (var point in points.AsSpan(1))
        {
            path.LineTo(point.Dx, point.Dy);
        }

        if (close)
        {
            path.Close();
        }

        return path;
    }

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not GlyphPainter old || old._kind != _kind || old._color != _color;
}
