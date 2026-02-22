using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Rawbit.UI.Adjustments.ViewModels;

namespace Rawbit.UI.Adjustments.Controls;

public class CurveEditor : Control
{
    private const double Padding = 6;
    private const double HandleRadius = 5;
    private const double MinGap = 0.02;
    public const int MaxPoints = 8;

    public static readonly StyledProperty<IList<CurvePoint>> PointsProperty =
        AvaloniaProperty.Register<CurveEditor, IList<CurvePoint>>(
            nameof(Points),
            defaultBindingMode: BindingMode.TwoWay);

    public IList<CurvePoint> Points
    {
        get => GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private DragHandle _dragging = DragHandle.None;
    private int _dragIndex = -1;
    private INotifyCollectionChanged? _observedCollection;

    static CurveEditor()
    {
        AffectsRender<CurveEditor>(PointsProperty);
    }

    public CurveEditor()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCaptureLost += OnPointerCaptureLost;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != PointsProperty)
            return;

        if (_observedCollection != null)
            _observedCollection.CollectionChanged -= OnPointsCollectionChanged;

        _observedCollection = Points as INotifyCollectionChanged;
        if (_observedCollection != null)
            _observedCollection.CollectionChanged += OnPointsCollectionChanged;

        InvalidateVisual();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var plot = GetPlotRect();
        var background = TryGetBrush("BrushPanel") ?? Brushes.Black;
        var border = TryGetBrush("BrushPanelBorder") ?? TryGetBrush("BrushBorder") ?? Brushes.Gray;
        var accent = TryGetBrush("BrushAccentActive") ?? Brushes.Orange;
        var muted = TryGetBrush("BrushTextMuted") ?? Brushes.Gray;

        context.FillRectangle(background, plot);
        context.DrawRectangle(new Pen(border, 1), plot);

        DrawGrid(context, plot, muted);
        DrawCurve(context, plot, accent);
        DrawHandles(context, plot, accent, border);
    }

    private void DrawGrid(DrawingContext context, Rect plot, IBrush gridBrush)
    {
        var pen = new Pen(gridBrush, 1);
        for (int i = 1; i <= 3; i++)
        {
            var t = i / 4.0;
            var x = plot.Left + plot.Width * t;
            var y = plot.Top + plot.Height * t;
            context.DrawLine(pen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            context.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
        }
    }

    // Draws the curve preview using the same math as the shader.
    private void DrawCurve(DrawingContext context, Rect plot, IBrush accent)
    {
        var points = GetSortedPoints();
        const int steps = 64;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i <= steps; i++)
            {
                var t = i / (double)steps;
                var y = EvaluateCurve(t, points);
                var p = PlotToScreen(plot, t, y);
                if (i == 0)
                    ctx.BeginFigure(p, false);
                else
                    ctx.LineTo(p);
            }
        }

        context.DrawGeometry(null, new Pen(accent, 2), geometry);
    }

    private void DrawHandles(DrawingContext context, Rect plot, IBrush fill, IBrush border)
    {
        var points = GetSortedPoints();
        foreach (var point in points)
        {
            var p = PlotToScreen(plot, point.X, point.Y);
            context.DrawEllipse(fill, new Pen(border, 1), p, HandleRadius, HandleRadius);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var plot = GetPlotRect();
        var points = GetSortedPoints();
        var hitIndex = FindHitPoint(points, point, plot);
        var isRightClick = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;

        if (isRightClick)
        {
            if (hitIndex >= 0)
            {
                Points.Remove(points[hitIndex]);
                InvalidateVisual();
            }
            e.Handled = true;
            return;
        }

        if (hitIndex >= 0)
        {
            _dragging = DragHandle.Point;
            _dragIndex = Points.IndexOf(points[hitIndex]);
        }
        else
        {
            if (Points.Count >= MaxPoints)
                return;

            var (x, y) = ScreenToPlot(plot, point);
            x = Clamp(x, MinGap, 1.0 - MinGap);
            y = Clamp(y, 0.0, 1.0);
            var insertIndex = FindInsertIndex(Points, x);
            Points.Insert(insertIndex, new CurvePoint(x, y));
            _dragging = DragHandle.Point;
            _dragIndex = insertIndex;
        }

        e.Pointer.Capture(this);
        UpdateFromPointer(point, plot);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging == DragHandle.None)
            return;

        var plot = GetPlotRect();
        var point = e.GetPosition(this);
        UpdateFromPointer(point, plot);
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragging == DragHandle.None)
            return;

        _dragging = DragHandle.None;
        _dragIndex = -1;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _dragging = DragHandle.None;
        _dragIndex = -1;
    }

    private void UpdateFromPointer(Point point, Rect plot)
    {
        var (x, y) = ScreenToPlot(plot, point);
        x = Clamp(x, 0, 1);
        y = Clamp(y, 0, 1);

        if (_dragging == DragHandle.Point && _dragIndex >= 0 && _dragIndex < Points.Count)
        {
            var left = _dragIndex == 0 ? 0.0 : Points[_dragIndex - 1].X;
            var right = _dragIndex == Points.Count - 1 ? 1.0 : Points[_dragIndex + 1].X;
            var newX = Clamp(x, left + MinGap, right - MinGap);
            Points[_dragIndex].X = newX;
            Points[_dragIndex].Y = y;
        }

        InvalidateVisual();
    }

    private Rect GetPlotRect()
    {
        var rect = Bounds.Deflate(Padding);
        var size = Math.Min(rect.Width, rect.Height);
        var left = rect.Left + (rect.Width - size) / 2;
        var top = rect.Top - Padding * 4;
        return new Rect(left, top, size, size);
    }

    private static Point PlotToScreen(Rect plot, double x, double y)
    {
        var sx = plot.Left + plot.Width * x;
        var sy = plot.Bottom - plot.Height * y;
        return new Point(sx, sy);
    }

    private static (double x, double y) ScreenToPlot(Rect plot, Point point)
    {
        var x = (point.X - plot.Left) / plot.Width;
        var y = (plot.Bottom - point.Y) / plot.Height;
        return (x, y);
    }

    // Curve preview math: mirrors ToneShader.sksl so the editor matches output.
    private static double EvaluateCurve(double x, IReadOnlyList<CurvePoint> points)
    {
        var total = points.Count + 2;
        Span<double> xs = stackalloc double[MaxPoints + 2];
        Span<double> ys = stackalloc double[MaxPoints + 2];

        xs[0] = 0.0;
        ys[0] = 0.0;
        for (var i = 0; i < points.Count; i++)
        {
            xs[i + 1] = Clamp(points[i].X, 0.001, 0.999);
            ys[i + 1] = Clamp(points[i].Y, 0.0, 1.0);
        }
        xs[total - 1] = 1.0;
        ys[total - 1] = 1.0;

        if (x <= xs[0])
            return ys[0];
        if (x >= xs[total - 1])
            return ys[total - 1];

        for (var i = 0; i < total - 1; i++)
        {
            if (x > xs[i + 1])
                continue;

            var p0 = Math.Max(i - 1, 0);
            var p1 = i;
            var p2 = i + 1;
            var p3 = Math.Min(i + 2, total - 1);

            var deltaBefore = (ys[p1] - ys[p0]) / Math.Max(0.001, xs[p1] - xs[p0]);
            var deltaCurrent = (ys[p2] - ys[p1]) / Math.Max(0.001, xs[p2] - xs[p1]);
            var deltaAfter = (ys[p3] - ys[p2]) / Math.Max(0.001, xs[p3] - xs[p2]);

            var tangentP1 = i == 0 ? deltaCurrent : (deltaBefore * deltaCurrent <= 0.0 ? 0.0 : (deltaBefore + deltaCurrent) * 0.5);
            var tangentP2 = i + 1 == total - 1 ? deltaCurrent : (deltaCurrent * deltaAfter <= 0.0 ? 0.0 : (deltaCurrent + deltaAfter) * 0.5);

            if (Math.Abs(deltaCurrent) > 0.0)
            {
                var alpha = tangentP1 / deltaCurrent;
                var beta = tangentP2 / deltaCurrent;
                var sum = alpha * alpha + beta * beta;
                if (sum > 9.0)
                {
                    var tau = 3.0 / Math.Sqrt(sum);
                    tangentP1 *= tau;
                    tangentP2 *= tau;
                }
            }

            return InterpolateCubicHermite(x, xs[p1], ys[p1], xs[p2], ys[p2], tangentP1, tangentP2);
        }

        return ys[total - 1];
    }

    // Hermite interpolation helper for the curve preview.
    private static double InterpolateCubicHermite(
        double x,
        double x1,
        double y1,
        double x2,
        double y2,
        double m1,
        double m2)
    {
        var dx = x2 - x1;
        if (dx <= 0.0)
            return y1;

        var t = (x - x1) / dx;
        var t2 = t * t;
        var t3 = t2 * t;
        var h00 = 2.0 * t3 - 3.0 * t2 + 1.0;
        var h10 = t3 - 2.0 * t2 + t;
        var h01 = -2.0 * t3 + 3.0 * t2;
        var h11 = t3 - t2;

        return h00 * y1 + h10 * m1 * dx + h01 * y2 + h11 * m2 * dx;
    }

    private static double Clamp(double value, double min, double max)
        => value < min ? min : (value > max ? max : value);

    private IBrush? TryGetBrush(string key)
    {
        if (Application.Current is { } app &&
            app.TryGetResource(key, ActualThemeVariant, out var value) &&
            value is IBrush brush)
        {
            return brush;
        }

        return null;
    }

    private static double Distance(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static int FindInsertIndex(IList<CurvePoint> points, double x)
    {
        for (var i = 0; i < points.Count; i++)
        {
            if (x < points[i].X)
                return i;
        }
        return points.Count;
    }

    private static int FindHitPoint(IReadOnlyList<CurvePoint> points, Point pos, Rect plot)
    {
        var bestIndex = -1;
        var bestDist = double.MaxValue;
        for (var i = 0; i < points.Count; i++)
        {
            var p = PlotToScreen(plot, points[i].X, points[i].Y);
            var d = Distance(pos, p);
            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }

        return bestDist <= HandleRadius * 2 ? bestIndex : -1;
    }

    private IReadOnlyList<CurvePoint> GetSortedPoints()
    {
        if (Points.Count == 0)
            return Array.Empty<CurvePoint>();

        var list = new List<CurvePoint>(Points);
        list.Sort((a, b) => a.X.CompareTo(b.X));
        return list;
    }

    private enum DragHandle
    {
        None,
        Point
    }
}
