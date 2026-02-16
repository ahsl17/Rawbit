using System;

namespace Rawbit.Models;

public class Adjustments
{
    public Guid AdjustmentsId { get; set; }
    public float Exposure { get; set; }
    public float Shadows { get; set; }
    public float Highlights { get; set; }
    public float Whites { get; set; }
    public float Blacks { get; set; }
    public float Temperature { get; set; }
    public float Tint { get; set; }
    public string HslAdjustmentsJson { get; set; } = "[]";
    public string CurvePointsJson { get; set; } = "[]";
    public int CurvePointCount { get; set; }
    
    public static Adjustments BuildWithDefaultValues() => new Adjustments
    {
        Exposure = 0,
        Shadows = 0,
        Highlights = 0,
        Whites = 0,
        Blacks = 0,
        Temperature = 0,
        Tint = 0,
        HslAdjustmentsJson = "[]",
        CurvePointsJson = "[]",
        CurvePointCount = 0
    };
}
