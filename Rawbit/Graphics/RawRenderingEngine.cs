using System;
using Rawbit.Graphics.Shaders;
using SkiaSharp;

namespace Rawbit.Graphics;

public class RawRenderingEngine : IDisposable
{
    private SKShader? _cachedShader;
    private float _lastExposure = float.NaN;
    private float _lastShadows = float.NaN;
    private float _lastHighlights = float.NaN;
    private int _lastCurvePointCount = -1;
    private int _lastCurvePointsHash;
    private int _lastHslHash;
    private SKImage? _lastSource;
    private readonly SKPaint _paint = new();

    public void Render(
        SKCanvas canvas,
        SKImage source,
        float exposure,
        float shadows,
        float highlights,
        float[] curvePoints,
        int curvePointCount,
        float[] hslAdjustments,
        float zoom,
        SKPoint pan,
        SKRect destRect)
    {
        var curveHash = HashCurvePoints(curvePoints, curvePointCount);
        var hslHash = HashFloatArray(hslAdjustments);
        // 1. Conditional rebuild: Only rebuild the shader if something actually changed (Performance!)
        if (_cachedShader == null || _lastSource != source ||
            Math.Abs(_lastExposure - exposure) > 0.001f ||
            Math.Abs(_lastShadows - shadows) > 0.001f ||
            Math.Abs(_lastHighlights - highlights) > 0.001f ||
            _lastCurvePointCount != curvePointCount ||
            _lastCurvePointsHash != curveHash ||
            _lastHslHash != hslHash)
        {
            _cachedShader?.Dispose();
            _cachedShader = CreateShader(source, exposure, shadows, highlights, curvePoints, curvePointCount, hslAdjustments);
            _lastSource = source;
            _lastExposure = exposure;
            _lastShadows = shadows;
            _lastHighlights = highlights;
            _lastCurvePointCount = curvePointCount;
            _lastCurvePointsHash = curveHash;
            _lastHslHash = hslHash;
        }

        // 2. Clear and Draw
        canvas.Clear(SKColors.Black);
        _paint.Shader = _cachedShader;
        _paint.IsAntialias = true;

        // Calculate uniform scaling (fit image to screen)
        var safeZoom = zoom;
        if (safeZoom <= 0f || float.IsNaN(safeZoom) || float.IsInfinity(safeZoom))
            safeZoom = 1f;
        float scale = Math.Min(destRect.Width / source.Width, destRect.Height / source.Height) * safeZoom;
        float x = (destRect.Width - source.Width * scale) / 2 + pan.X;
        float y = (destRect.Height - source.Height * scale) / 2 + pan.Y;

        canvas.Save();
        // These manipulate the Matrix Stack. Instead of calculating every pixel's position manually, you move the "paper" and draw a simple rectangle
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawRect(0, 0, source.Width, source.Height, _paint);
        canvas.Restore();
    }

    private SKShader CreateShader(
        SKImage source,
        float exposure,
        float shadows,
        float highlights,
        float[] curvePoints,
        int curvePointCount,
        float[] hslAdjustments)
    {
        using var imageShader = source.ToShader(SKShaderTileMode.Clamp,
            SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear));
        // A Uniform is a constant value for each pixel forwarded from the CPU to the GPU
        return ToneShader.CreateToneShader(
            source,
            exposure,
            shadows,
            highlights,
            curvePoints,
            curvePointCount,
            hslAdjustments);
    }

    private static int HashCurvePoints(float[] points, int count)
    {
        unchecked
        {
            var hash = 17;
            var length = Math.Min(points.Length, count * 2);
            for (var i = 0; i < length; i++)
                hash = hash * 31 + points[i].GetHashCode();
            return hash;
        }
    }

    private static int HashFloatArray(float[] points)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < points.Length; i++)
                hash = hash * 31 + points[i].GetHashCode();
            return hash;
        }
    }

    public void Dispose()
    {
        _cachedShader?.Dispose();
        _paint?.Dispose();
    } 
}
