using System;

namespace Rawbit.Models;

public class Image
{
    public Guid ImageId { get; set; } = Guid.Empty;
    public required string Path { get; set; }
    public required Adjustments Adjustments { get; set; }
}