using SkiaSharp;

namespace Rawbit.Graphics;

public readonly record struct RenderSettings(
    float Zoom,
    SKPoint Pan,
    SKRect DestRect);
