using System;

namespace Rawbit.Models;

public class Adjustments
{
    public Guid AdjustmentsId { get; set; }
    public float Exposure { get; set; }
    public float Shadows { get; set; }
    
    public static Adjustments BuildWithDefaultValues() => new Adjustments { Exposure = 0, Shadows = 0 };
}