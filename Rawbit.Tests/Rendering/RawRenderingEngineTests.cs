using System;
using System.Reflection;
using Rawbit.Graphics;
using Rawbit.Tests.Helpers;
using SkiaSharp;
using Xunit;

namespace Rawbit.Tests.Rendering;

public class RawRenderingEngineTests
{
    [Fact]
    public void Render_PassThroughWhenNeutral()
    {
        // GIVEN a small source image and a matching render target
        using var sourceBitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        sourceBitmap.SetPixel(0, 0, new SKColor(10, 20, 30, 255));
        sourceBitmap.SetPixel(1, 0, new SKColor(40, 50, 60, 255));
        sourceBitmap.SetPixel(0, 1, new SKColor(70, 80, 90, 255));
        sourceBitmap.SetPixel(1, 1, new SKColor(120, 130, 140, 255));

        using var sourceImage = SKImage.FromBitmap(sourceBitmap);
        using var surface = SKSurface.Create(new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul));
        var engine = new RawRenderingEngine();

        // WHEN rendering with neutral exposure and shadows
        var shader = RenderTestDefaults.Shader();
        var render = RenderTestDefaults.Render(0f, new SKPoint(0f, 0f), new SKRect(0, 0, 2, 2));
        engine.Render(surface.Canvas, new RenderRequest(sourceImage, shader, render));

        using var snapshot = surface.Snapshot();
        using var result = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        snapshot.ReadPixels(result.Info, result.GetPixels(), result.RowBytes, 0, 0);

        // THEN pixels should match the reference tone pipeline within tolerance
        AssertColorApprox(
            TonePipelineReference.Apply(sourceBitmap.GetPixel(0, 0), 0f, 0f, 0f, 0f, 0f, RenderTestDefaults.EmptyCurve, 0),
            result.GetPixel(0, 0));
        AssertColorApprox(
            TonePipelineReference.Apply(sourceBitmap.GetPixel(1, 0), 0f, 0f, 0f, 0f, 0f, RenderTestDefaults.EmptyCurve, 0),
            result.GetPixel(1, 0));
        AssertColorApprox(
            TonePipelineReference.Apply(sourceBitmap.GetPixel(0, 1), 0f, 0f, 0f, 0f, 0f, RenderTestDefaults.EmptyCurve, 0),
            result.GetPixel(0, 1));
        AssertColorApprox(
            TonePipelineReference.Apply(sourceBitmap.GetPixel(1, 1), 0f, 0f, 0f, 0f, 0f, RenderTestDefaults.EmptyCurve, 0),
            result.GetPixel(1, 1));
    }

    [Fact]
    public void Render_ReusesShaderForSameSourceAndExposure()
    {
        // GIVEN a source image and rendering engine
        using var sourceBitmap = new SKBitmap(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        sourceBitmap.SetPixel(0, 0, new SKColor(10, 20, 30, 255));
        using var sourceImage = SKImage.FromBitmap(sourceBitmap);
        using var surface = SKSurface.Create(new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul));
        var engine = new RawRenderingEngine();

        // WHEN rendering twice with the same inputs
        var shader = RenderTestDefaults.Shader();
        var render = RenderTestDefaults.Render(0f, new SKPoint(0f, 0f), new SKRect(0, 0, 2, 2));
        engine.Render(surface.Canvas, new RenderRequest(sourceImage, shader, render));
        var shader1 = GetCachedShader(engine);

        engine.Render(surface.Canvas, new RenderRequest(sourceImage, shader, render));
        var shader2 = GetCachedShader(engine);

        // THEN the cached shader instance is reused
        Assert.Same(shader1, shader2);

        // WHEN exposure changes
        var brighterShader = RenderTestDefaults.Shader(exposure: 1f);
        engine.Render(surface.Canvas, new RenderRequest(sourceImage, brighterShader, render));
        var shader3 = GetCachedShader(engine);

        // THEN the cached shader is rebuilt
        Assert.NotSame(shader2, shader3);
    }

    private static SKShader? GetCachedShader(RawRenderingEngine engine)
    {
        var field = typeof(RawRenderingEngine).GetField("_cachedShader", BindingFlags.NonPublic | BindingFlags.Instance);
        return (SKShader?)field?.GetValue(engine);
    }

    private static void AssertColorApprox(SKColor expected, SKColor actual, byte tolerance = 2)
    {
        Assert.InRange(Math.Abs(expected.Red - actual.Red), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Green - actual.Green), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Blue - actual.Blue), 0, tolerance);
        Assert.InRange(Math.Abs(expected.Alpha - actual.Alpha), 0, tolerance);
    }
}
