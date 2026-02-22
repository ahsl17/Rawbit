using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Rawbit.UI.Adjustments;

public partial class AdjustmentsView : UserControl
{
    private ScrollViewer? _thumbnailsScrollViewer;
    private ListBox? _thumbnailsList;
    private const double WheelScrollSpeed = 40;

    public AdjustmentsView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _thumbnailsList = this.FindControl<ListBox>("ThumbnailsList");
        _thumbnailsList?.AddHandler(InputElement.PointerWheelChangedEvent, OnThumbnailsWheel, 
            RoutingStrategies.Tunnel, true);
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_thumbnailsList != null)
        {
            _thumbnailsList.RemoveHandler(InputElement.PointerWheelChangedEvent, OnThumbnailsWheel);
        }

        _thumbnailsScrollViewer = null;
        _thumbnailsList = null;
    }

    private void OnThumbnailsWheel(object? sender, PointerWheelEventArgs e)
    {
        if (_thumbnailsList == null)
            return;

        _thumbnailsScrollViewer ??= _thumbnailsList.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();

        if (_thumbnailsScrollViewer == null)
            return;

        var offset = _thumbnailsScrollViewer.Offset;
        var delta = Math.Abs(e.Delta.X) > Math.Abs(e.Delta.Y) ? e.Delta.X : e.Delta.Y;
        if (Math.Abs(delta) < 0.001)
            return;

        var maxX = Math.Max(0, _thumbnailsScrollViewer.Extent.Width - _thumbnailsScrollViewer.Viewport.Width);
        var newX = Math.Clamp(offset.X - delta * WheelScrollSpeed, 0, maxX);
        _thumbnailsScrollViewer.Offset = new Vector(newX, offset.Y);
        e.Handled = true;
    }
}
