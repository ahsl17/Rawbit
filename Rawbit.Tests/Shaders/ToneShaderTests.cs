using System;
using Rawbit.Graphics.Shaders;
using Rawbit.Tests.Helpers;
using SkiaSharp;
using Xunit;

namespace Rawbit.Tests.Shaders;

public class ToneShaderTests
{
    [Fact]
    public void ToneShader_Compiles()
    {
        // GIVEN compiled SkSL is embedded and loadable
        // WHEN the static effect is accessed
        // THEN it should be available
        Assert.NotNull(ToneShader.ToneEffect);
    }

    [Fact]
    public void CreateToneShader_ReturnsShader()
    {
        // GIVEN a tiny input image
        using var bitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.SetPixel(0, 0, new SKColor(10, 20, 30, 255));
        bitmap.SetPixel(1, 0, new SKColor(40, 50, 60, 255));
        bitmap.SetPixel(0, 1, new SKColor(70, 80, 90, 255));
        bitmap.SetPixel(1, 1, new SKColor(120, 130, 140, 255));

        using var image = SKImage.FromBitmap(bitmap);
        // WHEN creating the tone shader
        var settings = RenderTestDefaults.Shader(
            curvePoints: PackCurve(Array.Empty<float>()),
            curvePointCount: 0);
        using var shader = ToneShader.CreateToneShader(image, settings);

        // THEN a shader instance is returned
        Assert.NotNull(shader);
    }

    [Fact]
    public void Exposure_IncreasesLuma()
    {
        // GIVEN a mid-gray pixel
        using var src = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(64, 64, 64, 255));
        using var image = SKImage.FromBitmap(src);

        // WHEN exposure is increased
        using var neutral = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f);
        using var brighter = RenderWithTone(image, exposure: 1f, shadows: 0f, highlights: 0f, whites: 0f);

        var lumaNeutral = Luma(neutral.GetPixel(0, 0));
        var lumaBrighter = Luma(brighter.GetPixel(0, 0));

        // THEN luma should increase
        Assert.True(lumaBrighter > lumaNeutral);
    }

    [Fact]
    public void Shadows_LiftsDarkPixelsMoreThanMidtones()
    {
        // GIVEN a dark and a midtone pixel
        using var src = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(20, 20, 20, 255));  // dark
        src.SetPixel(1, 0, new SKColor(128, 128, 128, 255)); // mid
        using var image = SKImage.FromBitmap(src);

        // WHEN shadows are lifted
        using var neutral = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f);
        using var lifted = RenderWithTone(image, exposure: 0f, shadows: 1f, highlights: 0f, whites: 0f);

        var darkNeutral = Luma(neutral.GetPixel(0, 0));
        var darkLifted = Luma(lifted.GetPixel(0, 0));
        var midNeutral = Luma(neutral.GetPixel(1, 0));
        var midLifted = Luma(lifted.GetPixel(1, 0));

        var darkDelta = darkLifted - darkNeutral;
        var midDelta = midLifted - midNeutral;

        // THEN dark pixels should be lifted more than midtones
        Assert.True(darkDelta > midDelta);
    }

    [Fact]
    public void ToneCurve_NonRegression_MatchesReference()
    {
        // GIVEN a mid-gray pixel and a custom curve
        using var src = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(128, 128, 128, 255));
        using var image = SKImage.FromBitmap(src);

        var curvePoints = new[] { 0.25f, 0.15f, 0.5f, 0.55f, 0.75f, 0.9f };
        const int curvePointCount = 3;

        // WHEN the shader renders with the curve
        using var rendered = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f, blacks: 0f, curvePoints: curvePoints, curvePointCount: curvePointCount);
        var actual = rendered.GetPixel(0, 0);

        // THEN it should match the reference tone pipeline
        var expected = TonePipelineReference.Apply(src.GetPixel(0, 0), 0f, 0f, 0f, 0f, 0f, PackCurve(curvePoints), curvePointCount);
        AssertColorApprox(expected, actual, tolerance: 3);
    }

    [Fact]
    public void Highlights_NegativeReducesBrightPixelLuma()
    {
        using var src = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(240, 240, 240, 255));
        using var image = SKImage.FromBitmap(src);

        using var neutral = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f);
        using var recovered = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: -1f, whites: 0f);

        Assert.True(Luma(recovered.GetPixel(0, 0)) < Luma(neutral.GetPixel(0, 0)));
    }

    [Fact]
    public void Whites_NegativeScalesLumaAcrossTones()
    {
        using var src = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(170, 170, 170, 255)); // upper-mid
        src.SetPixel(1, 0, new SKColor(250, 250, 250, 255)); // near-white
        using var image = SKImage.FromBitmap(src);

        using var neutral = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f);
        using var whitesDown = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: -1f);

        var upperMidRatio = Luma(whitesDown.GetPixel(0, 0)) / Math.Max(1.0f, Luma(neutral.GetPixel(0, 0)));
        var nearWhiteRatio = Luma(whitesDown.GetPixel(1, 0)) / Math.Max(1.0f, Luma(neutral.GetPixel(1, 0)));

        Assert.InRange(Math.Abs(upperMidRatio - nearWhiteRatio), 0.0, 0.08);
    }

    [Fact]
    public void Blacks_PositiveLiftsDarkPixelsMoreThanMidtones()
    {
        using var src = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
        src.SetPixel(0, 0, new SKColor(20, 20, 20, 255)); // dark
        src.SetPixel(1, 0, new SKColor(128, 128, 128, 255)); // mid
        using var image = SKImage.FromBitmap(src);

        using var neutral = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f, blacks: 0f);
        using var lifted = RenderWithTone(image, exposure: 0f, shadows: 0f, highlights: 0f, whites: 0f, blacks: 1f);

        var darkDelta = Luma(lifted.GetPixel(0, 0)) - Luma(neutral.GetPixel(0, 0));
        var midDelta = Luma(lifted.GetPixel(1, 0)) - Luma(neutral.GetPixel(1, 0));

        Assert.True(darkDelta > midDelta);
    }

    private static SKBitmap RenderWithTone(
        SKImage image,
        float exposure,
        float shadows,
        float highlights,
        float whites = 0f,
        float blacks = 0f,
        float[]? curvePoints = null,
        int curvePointCount = 0,
        float[]? hslAdjustments = null)
    {
        curvePoints = PackCurve(curvePoints);
        var hsl = hslAdjustments ?? RenderTestDefaults.EmptyHsl;
        var settings = RenderTestDefaults.Shader(
            exposure: exposure,
            shadows: shadows,
            highlights: highlights,
            whites: whites,
            blacks: blacks,
            curvePoints: curvePoints,
            curvePointCount: curvePointCount,
            hslAdjustments: hsl);
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

    private static float Luma(SKColor c) => 0.2126f * c.Red + 0.7152f * c.Green + 0.0722f * c.Blue;

    private static void AssertColorApprox(SKColor expected, SKColor actual, byte tolerance)
    {
        Assert.InRange(Math.Abs(expected.Red - actual.Red), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Green - actual.Green), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Blue - actual.Blue), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Alpha - actual.Alpha), 0, tolerance);
    }

    private static float[] PackCurve(float[]? points)
    {
        var packed = new float[16];
        if (points == null || points.Length == 0)
            return packed;

        var length = Math.Min(points.Length, packed.Length);
        Array.Copy(points, packed, length);
        return packed;
    }
}
