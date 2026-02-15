using System;
using Rawbit.Graphics.Shaders;
using Rawbit.Tests.Helpers;
using SkiaSharp;
using Xunit;

namespace Rawbit.Tests.Shaders;

public class ColorAdjustmentsShaderTests
{
    [Fact]
    public void Hsl_SaturationBoost_IncreasesChroma()
    {
        // GIVEN a saturated red pixel
        using var src = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(220, 40, 40, 255));
        using var image = SKImage.FromBitmap(src);

        var hsl = new float[24];
        hsl[1] = 50f; // Red saturation +50%

        // WHEN applying HSL saturation boost
        using var neutral = RenderWithTone(image, hslAdjustments: RenderTestDefaults.EmptyHsl);
        using var boosted = RenderWithTone(image, hslAdjustments: hsl);

        var satNeutral = Saturation(neutral.GetPixel(0, 0));
        var satBoosted = Saturation(boosted.GetPixel(0, 0));

        // THEN saturation should increase
        Assert.True(satBoosted > satNeutral);
    }

    private static SKBitmap RenderWithTone(SKImage image, float[] hslAdjustments)
    {
        var settings = RenderTestDefaults.Shader(
            curvePoints: RenderTestDefaults.EmptyCurve,
            curvePointCount: 0,
            hslAdjustments: hslAdjustments);
        using var shader = ToneShader.CreateToneShader(image, settings);
        using var surface = SKSurface.Create(new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint { Shader = shader, IsAntialias = false };
        surface.Canvas.DrawRect(0, 0, image.Width, image.Height, paint);

        using var snapshot = surface.Snapshot();
        var result = new SKBitmap(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        snapshot.ReadPixels(result.Info, result.GetPixels(), result.RowBytes, 0, 0);
        return result;
    }

    private static float Saturation(SKColor c)
    {
        var r = c.Red / 255f;
        var g = c.Green / 255f;
        var b = c.Blue / 255f;
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;
        if (max <= 0f) return 0f;
        return delta / max;
    }
}
