using System;
using SkiaSharp;

namespace Rawbit.Tests.Helpers;

internal static class TonePipelineReference
{
    public static SKColor Apply(
        SKColor input,
        float exposure,
        float shadows,
        float highlights,
        float whites,
        float blacks,
        float[] curvePoints,
        int curvePointCount)
    {
        var r = input.Red / 255f;
        var g = input.Green / 255f;
        var b = input.Blue / 255f;

        ApplyExposure(ref r, ref g, ref b, exposure);
        ApplyWhites(ref r, ref g, ref b, whites);
        ApplyBlacks(ref r, ref g, ref b, blacks);
        ApplyShadows(ref r, ref g, ref b, shadows);
        ApplyHighlights(ref r, ref g, ref b, highlights);
        ApplyAces(ref r, ref g, ref b);
        ApplyGamma(ref r, ref g, ref b);
        ApplyPointCurve(ref r, ref g, ref b, curvePoints, curvePointCount);

        r = Clamp01(r);
        g = Clamp01(g);
        b = Clamp01(b);

        return new SKColor(ToByte(r), ToByte(g), ToByte(b), input.Alpha);
    }

    public static float Luma(SKColor c)
        => 0.2126f * c.Red + 0.7152f * c.Green + 0.0722f * c.Blue;

    private static void ApplyExposure(ref float r, ref float g, ref float b, float ev)
    {
        var scale = MathF.Pow(2f, ev);
        r *= scale;
        g *= scale;
        b *= scale;
    }

    private static void ApplyShadows(ref float r, ref float g, ref float b, float amount)
    {
        var luma = GetLuma(r, g, b);
        var mask = Smoothstep(0.5f, 0.0f, luma);
        var scale = MathF.Pow(2f, amount * mask);
        r *= scale;
        g *= scale;
        b *= scale;
    }

    private static void ApplyHighlights(ref float r, ref float g, ref float b, float amount)
    {
        var highlightsAdj = Clamp(amount, -1f, 1f);
        if (MathF.Abs(highlightsAdj) < 1e-4f)
            return;

        var safeR = MathF.Max(r, 0f);
        var safeG = MathF.Max(g, 0f);
        var safeB = MathF.Max(b, 0f);
        var luma = GetLuma(safeR, safeG, safeB);
        var maskInput = TanhApprox(luma * 1.5f);
        var highlightMask = Smoothstep(0.3f, 0.95f, maskInput);
        if (highlightMask < 1e-3f)
            return;

        float adjustedR;
        float adjustedG;
        float adjustedB;

        if (highlightsAdj < 0f)
        {
            float newLuma;
            if (luma <= 1f)
            {
                var gamma = 1f - highlightsAdj * 1.75f;
                newLuma = MathF.Pow(MathF.Max(luma, 0f), gamma);
            }
            else
            {
                var lumaExcess = luma - 1f;
                var compressionStrength = (-highlightsAdj) * 6f;
                var compressedExcess = lumaExcess / (1f + lumaExcess * compressionStrength);
                newLuma = 1f + compressedExcess;
            }

            var tonalScale = newLuma / MathF.Max(luma, 1e-4f);
            var tonallyAdjustedR = r * tonalScale;
            var tonallyAdjustedG = g * tonalScale;
            var tonallyAdjustedB = b * tonalScale;

            var desaturationAmount = Smoothstep(1f, 10f, luma);
            adjustedR = Lerp(tonallyAdjustedR, newLuma, desaturationAmount);
            adjustedG = Lerp(tonallyAdjustedG, newLuma, desaturationAmount);
            adjustedB = Lerp(tonallyAdjustedB, newLuma, desaturationAmount);
        }
        else
        {
            var factor = MathF.Pow(2f, highlightsAdj * 1.75f);
            adjustedR = r * factor;
            adjustedG = g * factor;
            adjustedB = b * factor;
        }

        r = Lerp(r, adjustedR, highlightMask);
        g = Lerp(g, adjustedG, highlightMask);
        b = Lerp(b, adjustedB, highlightMask);
    }

    private static void ApplyWhites(ref float r, ref float g, ref float b, float amount)
    {
        var whitesAdj = Clamp(amount, -1f, 1f);
        if (MathF.Abs(whitesAdj) < 1e-4f)
            return;

        var whiteLevel = 1f - whitesAdj * 0.25f;
        var scale = 1f / MathF.Max(whiteLevel, 0.01f);
        r *= scale;
        g *= scale;
        b *= scale;
    }

    private static void ApplyBlacks(ref float r, ref float g, ref float b, float amount)
    {
        var blacksAdj = Clamp(amount, -1f, 1f);
        if (MathF.Abs(blacksAdj) < 1e-4f)
            return;

        var luma = GetLuma(MathF.Max(r, 0f), MathF.Max(g, 0f), MathF.Max(b, 0f));
        var mask = 1f - Smoothstep(0f, 0.25f, luma);
        if (mask < 1e-3f)
            return;

        var adjustment = blacksAdj * 0.75f;
        var factor = MathF.Pow(2f, adjustment);

        var adjustedR = r * factor;
        var adjustedG = g * factor;
        var adjustedB = b * factor;

        r = Lerp(r, adjustedR, mask);
        g = Lerp(g, adjustedG, mask);
        b = Lerp(b, adjustedB, mask);
    }

    private static void ApplyAces(ref float r, ref float g, ref float b)
    {
        r = AcesChannel(r);
        g = AcesChannel(g);
        b = AcesChannel(b);
    }

    private static void ApplyGamma(ref float r, ref float g, ref float b)
    {
        r = MathF.Pow(MathF.Max(r, 0f), 1f / 2.2f);
        g = MathF.Pow(MathF.Max(g, 0f), 1f / 2.2f);
        b = MathF.Pow(MathF.Max(b, 0f), 1f / 2.2f);
    }

    private static void ApplyPointCurve(ref float r, ref float g, ref float b, float[] curvePoints, int curvePointCount)
    {
        var luma = GetLuma(r, g, b);
        var newLuma = ApplyPointCurve1(luma, curvePoints, curvePointCount);
        var scale = newLuma / MathF.Max(luma, 1e-5f);
        r *= scale;
        g *= scale;
        b *= scale;
    }

    private static float ApplyPointCurve1(float x, float[] curvePoints, int curvePointCount)
    {
        const int MaxPoints = 8;
        var count = Math.Clamp(curvePointCount, 0, MaxPoints);
        var total = count + 2;
        var lastIndex = count + 1;

        if (x <= 0f)
            return 0f;
        if (x >= 1f)
            return 1f;

        for (var i = 0; i < 9; i++)
        {
            if (i >= total - 1)
                break;

            var p1 = GetCurvePoint(i, count, curvePoints);
            var p2 = GetCurvePoint(i + 1, count, curvePoints);
            if (x > p2.x)
                continue;

            var p0 = GetCurvePoint(i - 1, count, curvePoints);
            var p3 = GetCurvePoint(i + 2, count, curvePoints);

            var deltaBefore = (p1.y - p0.y) / MathF.Max(0.001f, p1.x - p0.x);
            var deltaCurrent = (p2.y - p1.y) / MathF.Max(0.001f, p2.x - p1.x);
            var deltaAfter = (p3.y - p2.y) / MathF.Max(0.001f, p3.x - p2.x);

            var tangentP1 = i == 0 ? deltaCurrent : (deltaBefore * deltaCurrent <= 0f ? 0f : (deltaBefore + deltaCurrent) * 0.5f);
            var tangentP2 = i + 1 == lastIndex ? deltaCurrent : (deltaCurrent * deltaAfter <= 0f ? 0f : (deltaCurrent + deltaAfter) * 0.5f);

            if (deltaCurrent != 0f)
            {
                var alpha = tangentP1 / deltaCurrent;
                var beta = tangentP2 / deltaCurrent;
                if (alpha * alpha + beta * beta > 9f)
                {
                    var tau = 3f / MathF.Sqrt(alpha * alpha + beta * beta);
                    tangentP1 *= tau;
                    tangentP2 *= tau;
                }
            }

            return Clamp01(InterpolateCubicHermite(x, p1, p2, tangentP1, tangentP2));
        }

        return 1f;
    }

    private static (float x, float y) GetCurvePoint(int idx, int count, float[] curvePoints)
    {
        if (idx <= 0)
            return (0f, 0f);

        var lastIndex = count + 1;
        if (idx >= lastIndex)
            return (1f, 1f);

        var ci = idx - 1;
        var baseIndex = ci * 2;
        var x = baseIndex < curvePoints.Length ? curvePoints[baseIndex] : 1f;
        var y = baseIndex + 1 < curvePoints.Length ? curvePoints[baseIndex + 1] : 1f;
        return (Clamp(x, 0.001f, 0.999f), Clamp(y, 0f, 1f));
    }

    private static float InterpolateCubicHermite(float x, (float x, float y) p1, (float x, float y) p2, float m1, float m2)
    {
        var dx = p2.x - p1.x;
        if (dx <= 0f)
            return p1.y;

        var t = (x - p1.x) / dx;
        var t2 = t * t;
        var t3 = t2 * t;
        var h00 = 2f * t3 - 3f * t2 + 1f;
        var h10 = t3 - 2f * t2 + t;
        var h01 = -2f * t3 + 3f * t2;
        var h11 = t3 - t2;
        return h00 * p1.y + h10 * m1 * dx + h01 * p2.y + h11 * m2 * dx;
    }

    private static float GetLuma(float r, float g, float b)
        => 0.2126f * r + 0.7152f * g + 0.0722f * b;

    private static float Smoothstep(float edge0, float edge1, float x)
    {
        var t = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private static float AcesChannel(float x)
    {
        const float a = 2.51f;
        const float b = 0.03f;
        const float c = 2.43f;
        const float d = 0.59f;
        const float e = 0.14f;
        return Clamp01((x * (a * x + b)) / (x * (c * x + d) + e));
    }

    private static byte ToByte(float value)
        => (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);

    private static float Clamp01(float value)
        => Clamp(value, 0f, 1f);

    private static float Clamp(float value, float min, float max)
        => value < min ? min : (value > max ? max : value);

    private static float Lerp(float a, float b, float t)
        => a + (b - a) * t;

    private static float TanhApprox(float x)
    {
        var e = MathF.Exp(2f * x);
        return (e - 1f) / (e + 1f);
    }
}
