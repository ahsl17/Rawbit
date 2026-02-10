using System;

namespace Rawbit.Models;

public class Image
{
    public Guid ImageId { get; set; }
    public string Path { get; set; }
    
    public Adjustments Adjustments { get; set; }
}