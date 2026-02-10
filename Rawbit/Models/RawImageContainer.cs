using System;
using SkiaSharp;

namespace Rawbit.Models;

public record RawImageContainer(SKImage? FullRes, SKImage? Proxy, SKSize Size) : IDisposable
{
    public void Dispose()
    {
        FullRes?.Dispose();
        Proxy?.Dispose();
    }
}