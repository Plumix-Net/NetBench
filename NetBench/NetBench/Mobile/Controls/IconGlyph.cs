using Avalonia;
using Avalonia.Media;
using Plumix.Rendering;
using Plumix.Widgets;

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
        var brush = new SolidColorBrush(_color);
        var w = size.Width;
        var h = size.Height;

        switch (_kind)
        {
            case GlyphKind.Play:
                context.DrawPolygon(brush, null,
                [
                    new Point(0, 0),
                    new Point(w, h / 2),
                    new Point(0, h),
                ]);
                break;

            case GlyphKind.Stop:
                context.DrawRectangle(brush, null, new Rect(0, 0, w, h));
                break;

            case GlyphKind.Plus:
            {
                var pen = StrokePen(brush, w * 2 / 14);
                context.DrawLine(pen, new Point(w / 2, 0), new Point(w / 2, h));
                context.DrawLine(pen, new Point(0, h / 2), new Point(w, h / 2));
                break;
            }

            case GlyphKind.Share:
            {
                // Стрелка вверх над лотком — как в макете (viewBox 14x14)
                var pen = StrokePen(brush, w * 1.6 / 14);
                var u = w / 14;
                context.DrawLine(pen, new Point(7 * u, 0.5 * u), new Point(7 * u, 9 * u));
                context.DrawLine(pen, new Point(3 * u, 4 * u), new Point(7 * u, 0.5 * u));
                context.DrawLine(pen, new Point(11 * u, 4 * u), new Point(7 * u, 0.5 * u));
                var tray = new PolylineGeometry(
                    [
                        new Point(1 * u, 9 * u),
                        new Point(1 * u, 13 * u),
                        new Point(13 * u, 13 * u),
                        new Point(13 * u, 9 * u),
                    ],
                    isFilled: false);
                context.DrawGeometry(null, pen, tray);
                break;
            }

            case GlyphKind.Sun:
            {
                var center = new Point(w / 2, h / 2);
                context.DrawCircle(brush, null, center, w * 3.1 / 16);
                var pen = StrokePen(brush, w * 1.4 / 16);
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var (sin, cos) = Math.SinCos(angle);
                    var inner = new Point(center.X + cos * w * 0.31, center.Y + sin * h * 0.31);
                    var outer = new Point(center.X + cos * w * 0.46, center.Y + sin * h * 0.46);
                    context.DrawLine(pen, inner, outer);
                }

                break;
            }

            case GlyphKind.ChevronRight:
            {
                var pen = StrokePen(brush, w * 1.6 / 12);
                var chevron = new PolylineGeometry(
                    [
                        new Point(w * 0.35, h * 0.15),
                        new Point(w * 0.7, h * 0.5),
                        new Point(w * 0.35, h * 0.85),
                    ],
                    isFilled: false);
                context.DrawGeometry(null, pen, chevron);
                break;
            }

            case GlyphKind.Moon:
            {
                // Полумесяц: из большого круга вырезан смещённый круг
                var full = new EllipseGeometry(new Rect(0, 0, w, h));
                var bite = new EllipseGeometry(new Rect(w * 0.25, -h * 0.25, w, h));
                var crescent = new CombinedGeometry(GeometryCombineMode.Exclude, full, bite);
                context.DrawGeometry(brush, null, crescent);
                break;
            }
        }
    }

    private static Pen StrokePen(IBrush brush, double thickness) => new(
        brush,
        thickness,
        lineCap: PenLineCap.Round,
        lineJoin: PenLineJoin.Round);

    public override bool ShouldRepaint(CustomPainter oldDelegate) =>
        oldDelegate is not GlyphPainter old || old._kind != _kind || old._color != _color;
}
