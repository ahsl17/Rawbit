using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Rawbit.Graphics;
using Rawbit.UI.ViewModels;
using SkiaSharp;

namespace Rawbit.UI.Views.Controls;

public class RawPreviewControl : Control
{
    private readonly RawRenderingEngine _engine = new();
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 8f;
    private float _zoom = 1f;
    private Vector _pan;
    private Models.RawImageContainer? _lastContainer;
    private bool _isZooming;
    private readonly DispatcherTimer _zoomDebounceTimer;

    public static readonly StyledProperty<AdjustmentsViewModel> ViewModelProperty =
        AvaloniaProperty.Register<RawPreviewControl, AdjustmentsViewModel>(nameof(ViewModel));

    public AdjustmentsViewModel ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public RawPreviewControl()
    {
        PointerWheelChanged += OnPointerWheelChanged;
        
        // Timer to handle switching back from ProxyImage to High-Res after zooming stops
        _zoomDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _zoomDebounceTimer.Tick += (_, _) =>
        {
            _zoomDebounceTimer.Stop();
            _isZooming = false;
            InvalidateVisual();
        };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ViewModel.RequestRedraw = () => Dispatcher.UIThread.Post(InvalidateVisual);
    }

    public override void Render(DrawingContext context)
    {
        var container = ViewModel.CurrentContainer;
        if (!ReferenceEquals(_lastContainer, container))
        {
            _lastContainer = container;
            _zoom = 1f;
            _pan = default;
        }

        // Draw proxy image during interaction for performance, otherwise full render
        var imageOverride = _isZooming ? ViewModel.ProxyImage : null;
        context.Custom(new RenderingEngineToViewConnector(ViewModel, _engine, Bounds, _zoom, _pan, imageOverride));
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var image = ViewModel.ActiveImage;
        if (image == null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 0.001) return;

        var oldZoom = _zoom;
        var newZoom = (float)Math.Clamp(oldZoom * Math.Pow(1.1, delta), MinZoom, MaxZoom);
        if (Math.Abs(newZoom - oldZoom) < 0.0001f) return;

        var fitScaleOld = GetFitScale(Bounds.Size, image) * oldZoom;
        var fitScaleNew = GetFitScale(Bounds.Size, image) * newZoom;

        var pointer = e.GetPosition(this);
        var baseOld = GetBaseOffset(Bounds.Size, image, fitScaleOld);
        var baseNew = GetBaseOffset(Bounds.Size, image, fitScaleNew);

        // Calculate image-relative coordinates to keep zoom centered on cursor
        var imgX = (pointer.X - baseOld.X - _pan.X) / fitScaleOld;
        var imgY = (pointer.Y - baseOld.Y - _pan.Y) / fitScaleOld;

        var newPanX = pointer.X - baseNew.X - imgX * fitScaleNew;
        var newPanY = pointer.Y - baseNew.Y - imgY * fitScaleNew;

        _pan = ClampPan(Bounds.Size, image, newZoom, new Vector(newPanX, newPanY));
        _zoom = newZoom;

        // Visual State Management
        _isZooming = true;
        _zoomDebounceTimer.Stop();
        _zoomDebounceTimer.Start();
        
        e.Handled = true;
        InvalidateVisual();
    }

    private static float GetFitScale(Size bounds, SKImage image) =>
        (float)Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);

    private static Vector GetBaseOffset(Size bounds, SKImage image, float scale) =>
        new((bounds.Width - image.Width * scale) / 2.0, (bounds.Height - image.Height * scale) / 2.0);

    private static Vector ClampPan(Size bounds, SKImage image, float zoom, Vector pan)
    {
        var scale = GetFitScale(bounds, image) * zoom;
        var maxPanX = Math.Max(0.0, (image.Width * scale - bounds.Width) / 2.0);
        var maxPanY = Math.Max(0.0, (image.Height * scale - bounds.Height) / 2.0);

        return new Vector(Math.Clamp(pan.X, -maxPanX, maxPanX), Math.Clamp(pan.Y, -maxPanY, maxPanY));
    }
}