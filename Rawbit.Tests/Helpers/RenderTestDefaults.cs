using Rawbit.Graphics;
using SkiaSharp;

namespace Rawbit.Tests.Helpers;

internal static class RenderTestDefaults
{
    public static readonly float[] EmptyCurve = new float[16];
    public static readonly float[] EmptyHsl = new float[24];

    public static ShaderSettings Shader(
        float exposure = 0f,
        float shadows = 0f,
        float highlights = 0f,
        float whites = 0f,
        float blacks = 0f,
        float temperature = 0f,
        float tint = 0f,
        float[]? curvePoints = null,
        int curvePointCount = 0,
        float[]? hslAdjustments = null)
    {
        return new ShaderSettings(
            exposure,
            shadows,
            highlights,
            whites,
            blacks,
            temperature,
            tint,
            curvePoints ?? EmptyCurve,
            curvePointCount,
            hslAdjustments ?? EmptyHsl);
    }

    public static RenderSettings Render(
        float zoom,
        SKPoint pan,
        SKRect destRect)
    {
        return new RenderSettings(zoom, pan, destRect);
    }
}
