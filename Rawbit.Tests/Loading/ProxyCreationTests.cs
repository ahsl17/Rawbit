using System;
using System.Reflection;
using Rawbit.Services;
using SkiaSharp;
using Xunit;

namespace Rawbit.Tests.Loading;

public class ProxyCreationTests
{
    [Fact]
    public void CreateProxy_Landscape_UsesLongSide()
    {
        // GIVEN a landscape image
        using var full = new SKBitmap(4000, 2000, SKColorType.Rgba8888, SKAlphaType.Premul);
        // WHEN creating a proxy
        using var proxy = InvokeCreateProxy(full);

        // THEN the long side is 1024 and aspect ratio is preserved
        Assert.Equal(1024, proxy.Width);
        Assert.Equal(512, proxy.Height);
    }

    [Fact]
    public void CreateProxy_Portrait_UsesLongSide()
    {
        // GIVEN a portrait image
        using var full = new SKBitmap(2000, 4000, SKColorType.Rgba8888, SKAlphaType.Premul);
        // WHEN creating a proxy
        using var proxy = InvokeCreateProxy(full);

        // THEN the long side is 1024 and aspect ratio is preserved
        Assert.Equal(512, proxy.Width);
        Assert.Equal(1024, proxy.Height);
    }

    private static SKBitmap InvokeCreateProxy(SKBitmap full)
    {
        var method = typeof(RawLoaderService).GetMethod("CreateProxy", BindingFlags.NonPublic | BindingFlags.Static);
        if (method == null) throw new InvalidOperationException("CreateProxy method not found.");

        return (SKBitmap)method.Invoke(null, new object[] { full })!;
    }
}
