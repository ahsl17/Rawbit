using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Rawbit.Graphics;
using Rawbit.UI.Adjustments.ViewModels;
using SkiaSharp;

namespace Rawbit.UI.Adjustments;

public class RenderingEngineToViewConnector : ICustomDrawOperation
{
    private readonly AdjustmentsViewModel _vm;
    private readonly RawRenderingEngine _engine;
    private readonly float _zoom;
    private readonly Vector _pan;
    private readonly SKImage? _imageOverride;
    public Rect Bounds { get; }

    public RenderingEngineToViewConnector(
        AdjustmentsViewModel vm,
        RawRenderingEngine engine,
        Rect bounds,
        float zoom,
        Vector pan,
        SKImage? imageOverride)
    {
        _vm = vm;
        _engine = engine;
        Bounds = bounds;
        _zoom = zoom;
        _pan = pan;
        _imageOverride = imageOverride;
    }

    public void Render(ImmediateDrawingContext context)
    {
        // The least asks avalonia to temporarily lend the use of the gpu and the skia canvas
        var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (lease == null) return;

        using var skiaContext = lease.Lease();
        
        // Ask the VM which image to use (Proxy or FullRes)
        var image = _imageOverride ?? _vm.ActiveImage;
        if (image != null)
        {
            var shader = new ShaderSettings(
                (float)_vm.LightAdjustments.ExposureValue,
                (float)_vm.LightAdjustments.ShadowsValue,
                (float)_vm.LightAdjustments.HighlightsValue,
                (float)_vm.LightAdjustments.TemperatureValue,
                (float)_vm.LightAdjustments.TintValue,
                _vm.ToneCurveAdjustment.CurvePointsPacked,
                _vm.ToneCurveAdjustment.CurvePointCount,
                _vm.HslAdjustments.AdjustmentsPacked);

            var render = new RenderSettings(
                _zoom,
                new SKPoint((float)_pan.X, (float)_pan.Y),
                Bounds.ToSKRect());

            var request = new RenderRequest(image, shader, render);
            _engine.Render(skiaContext.SkCanvas, request);
        }
    }

    public void Dispose() { /* Engine is managed by the Control */ }
    public bool HitTest(Point p) => Bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) => false;
}
