using System;
using System.Linq;
using Rawbit.Models;

namespace Rawbit.Graphics;

public sealed record ShaderSettings(
    float Exposure,
    float Shadows,
    float Highlights,
    float Whites,
    float Blacks,
    float Temperature,
    float Tint,
    float[] CurvePoints,
    int CurvePointCount,
    float[] HslAdjustments)
{
    public static ShaderSettings From(AdjustmentsState state, bool cloneArrays = true)
    {
        var curve = cloneArrays ? state.CurvePoints.ToArray() : state.CurvePoints;
        var hsl = cloneArrays ? state.Hsl.ToArray() : state.Hsl;

        return new ShaderSettings(
            state.Exposure,
            state.Shadows,
            state.Highlights,
            state.Whites,
            state.Blacks,
            state.Temperature,
            state.Tint,
            curve,
            state.CurvePointCount,
            hsl);
    }

    public int ComputeHash()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Exposure.GetHashCode();
            hash = hash * 31 + Shadows.GetHashCode();
            hash = hash * 31 + Highlights.GetHashCode();
            hash = hash * 31 + Whites.GetHashCode();
            hash = hash * 31 + Blacks.GetHashCode();
            hash = hash * 31 + Temperature.GetHashCode();
            hash = hash * 31 + Tint.GetHashCode();
            hash = hash * 31 + CurvePointCount;
            hash = hash * 31 + HashArray(CurvePoints, Math.Min(CurvePoints.Length, CurvePointCount * 2));
            hash = hash * 31 + HashArray(HslAdjustments, HslAdjustments.Length);
            return hash;
        }
    }

    private static int HashArray(float[] values, int length)
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < length; i++)
                hash = hash * 31 + values[i].GetHashCode();
            return hash;
        }
    }
}
