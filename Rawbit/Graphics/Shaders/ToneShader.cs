using System;
using System.IO;
using System.Reflection;
using SkiaSharp;

namespace Rawbit.Graphics.Shaders;

public static partial class ToneShader
{
    // Define Constants for Uniform Names to avoid magic strings
    public static readonly SKRuntimeEffect ToneEffect;

    static ToneShader()
    {
        ToneEffect = CompileEffect("ToneShader.sksl");
    }

    private static SKRuntimeEffect CompileEffect(string resourceName)
    {
        var source = ReadShaderFromResource(resourceName);

        // Compile
        var effect = SKRuntimeEffect.CreateShader(source, out var errors);

        if (!string.IsNullOrEmpty(errors))
        {
            Console.WriteLine($"Shader Compilation Error ({resourceName}): {errors}");
            throw new InvalidOperationException($"SkSL Compilation Failed: {errors}");
        }

        return effect;
    }

    private static string ReadShaderFromResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"Rawbit.Graphics.Shaders.{resourceName}");
        if (stream == null) throw new FileNotFoundException($"Shader {resourceName} not found.");

        using var reader = new StreamReader(stream);
        string source = reader.ReadToEnd();
        return source;
    }

    public static SKShader CreateToneShader(
        SKImage source,
        float exposure,
        float shadows,
        float highlights,
        float[] curvePoints,
        int curvePointCount)
    {
        // A Uniform is a constant value for each pixel forwarded from the CPU to the GPU
        var uniforms = new SKRuntimeEffectUniforms(ToneEffect)
        {
            { Uniforms.Exposure, exposure },
            { Uniforms.Shadows, shadows },
            { Uniforms.Highlights, highlights },
            { Uniforms.CurvePoints, curvePoints },
            { Uniforms.CurvePointCount, (float)curvePointCount }
        };

        var children = new SKRuntimeEffectChildren(ToneEffect)
        {
            { Uniforms.InputImage, source.ToShader() }
        };

        return ToneEffect.ToShader(uniforms, children);
    }
}
