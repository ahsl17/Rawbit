using System;
using Rawbit.Graphics.Shaders;
using SkiaSharp;

namespace Rawbit.Graphics;

public class RawRenderingEngine : IDisposable
{
    private SKShader? _cachedShader;
    private int _lastSettingsHash;
    private SKImage? _lastSource;
    private readonly SKPaint _paint = new();

    public void Render(SKCanvas canvas, RenderRequest request)
    {
        var source = request.Source;
        var settingsHash = request.Shader.ComputeHash();
        // 1. Conditional rebuild: Only rebuild the shader if something actually changed (Performance!)
        if (_cachedShader == null || _lastSource != source || _lastSettingsHash != settingsHash)
        {
            _cachedShader?.Dispose();
            _cachedShader = CreateShader(source, request.Shader);
            _lastSource = source;
            _lastSettingsHash = settingsHash;
        }

        // 2. Clear and Draw
        canvas.Clear(SKColors.Black);
        _paint.Shader = _cachedShader;
        _paint.IsAntialias = true;

        // Calculate uniform scaling (fit image to screen)
        var safeZoom = request.Render.Zoom;
        if (safeZoom <= 0f || float.IsNaN(safeZoom) || float.IsInfinity(safeZoom))
            safeZoom = 1f; 
        var destRect = request.Render.DestRect;
        float scale = Math.Min(destRect.Width / source.Width, destRect.Height / source.Height) * safeZoom;
        float x = (destRect.Width - source.Width * scale) / 2 + request.Render.Pan.X;
        float y = (destRect.Height - source.Height * scale) / 2 + request.Render.Pan.Y;

        canvas.Save();
        // These manipulate the Matrix Stack. Instead of calculating every pixel's position manually, you move the "paper" and draw a simple rectangle
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawRect(0, 0, source.Width, source.Height, _paint);
        canvas.Restore();
    }

    private SKShader CreateShader(SKImage source, ShaderSettings settings)
    {
        using var imageShader = source.ToShader(SKShaderTileMode.Clamp,
            SKShaderTileMode.Clamp, new SKSamplingOptions(SKFilterMode.Linear));
        // A Uniform is a constant value for each pixel forwarded from the CPU to the GPU
        return ToneShader.CreateToneShader(source, settings);
    }

    public void Dispose()
    {
        _cachedShader?.Dispose();
        _paint?.Dispose();
    } 
}
