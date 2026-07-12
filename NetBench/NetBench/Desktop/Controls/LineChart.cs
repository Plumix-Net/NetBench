using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Specialized;

namespace NetBench.Desktop.Controls;

public class LineChart : Control
{
    public static readonly StyledProperty<ObservableCollection<ChartPoint>?> PointsProperty =
        AvaloniaProperty.Register<LineChart, ObservableCollection<ChartPoint>?>(nameof(Points));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<LineChart, IBrush?>(nameof(Stroke), Brushes.DodgerBlue);

    public ObservableCollection<ChartPoint>? Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    static LineChart()
    {
        AffectsRender<LineChart>(StrokeProperty);
        PointsProperty.Changed.AddClassHandler<LineChart>((c, e) =>
        {
            if (e.OldValue is ObservableCollection<ChartPoint> old)
                old.CollectionChanged -= c.OnCollectionChanged;
            if (e.NewValue is ObservableCollection<ChartPoint> next)
                next.CollectionChanged += c.OnCollectionChanged;
            c.InvalidateVisual();
        });
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    public override void Render(DrawingContext ctx)
    {
        var pts = Points;
        var w = Bounds.Width;
        var h = Bounds.Height;

        if (pts == null || pts.Count < 2 || w <= 0 || h <= 0)
            return;

        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = 0.0;

        foreach (var p in pts)
        {
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y > maxY) maxY = p.Y;
        }

        if (maxX <= minX) maxX = minX + 1;
        if (maxY <= 0) maxY = 1;

        const double padV = 0.08;
        var pen = new Pen(Stroke ?? Brushes.DodgerBlue, 2.5,
            lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

        Point Map(ChartPoint p) => new Point(
            (p.X - minX) / (maxX - minX) * w,
            h - (p.Y / maxY) * h * (1 - padV * 2) - h * padV
        );

        for (var i = 1; i < pts.Count; i++)
            ctx.DrawLine(pen, Map(pts[i - 1]), Map(pts[i]));
    }
}
