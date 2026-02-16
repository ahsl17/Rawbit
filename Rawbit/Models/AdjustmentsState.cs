namespace Rawbit.Models;

public sealed record AdjustmentsState(
    float Exposure,
    float Shadows,
    float Highlights,
    float Whites,
    float Blacks,
    float Temperature,
    float Tint,
    float[] Hsl,
    float[] CurvePoints,
    int CurvePointCount);
