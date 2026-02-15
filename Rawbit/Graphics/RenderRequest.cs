using SkiaSharp;

namespace Rawbit.Graphics;

public sealed record RenderRequest(
    SKImage Source,
    ShaderSettings Shader,
    RenderSettings Render);
