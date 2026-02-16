using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Rawbit.Data.DbContext;
using Rawbit.Data.Repositories.Interfaces;
using Rawbit.Models;

namespace Rawbit.Data.Repositories;

public class ImageRepository : IImageRepository
{
    private readonly RawbitProjectContext _dbContext;

    public ImageRepository(RawbitProjectContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<string>> GetImagesToRegisterAsync(List<string> imagesPath)
    {
        var alreadyRegistered = await _dbContext.Images
            .Where(i => imagesPath.Contains(i.Path))
            .Select(i => i.Path)
            .ToListAsync()
            .ConfigureAwait(false);
        var imagesToRegister = imagesPath.Except(alreadyRegistered);
        return imagesToRegister.ToList();
    }
    
    public async Task RegisterImagesAsync(IEnumerable<string> images)
    {
        await _dbContext.AddRangeAsync(images.Select(i => new Image { Path = i, Adjustments = Adjustments.BuildWithDefaultValues()})).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<Adjustments?> GetAdjustmentsByPathAsync(string imagePath)
    {
        var image = await _dbContext.Images
            .Include(i => i.Adjustments)
            .FirstOrDefaultAsync(i => i.Path == imagePath)
            .ConfigureAwait(false);
        return image?.Adjustments;
    }

    public async Task UpdateAdjustmentsByPathAsync(string imagePath, Adjustments adjustments)
    {
        var image = await _dbContext.Images
            .Include(i => i.Adjustments)
            .FirstOrDefaultAsync(i => i.Path == imagePath)
            .ConfigureAwait(false);
        if (image?.Adjustments == null)
            return;

        var target = image.Adjustments;
        target.Exposure = adjustments.Exposure;
        target.Shadows = adjustments.Shadows;
        target.Highlights = adjustments.Highlights;
        target.Whites = adjustments.Whites;
        target.Blacks = adjustments.Blacks;
        target.Temperature = adjustments.Temperature;
        target.Tint = adjustments.Tint;
        target.HslAdjustmentsJson = adjustments.HslAdjustmentsJson;
        target.CurvePointsJson = adjustments.CurvePointsJson;
        target.CurvePointCount = adjustments.CurvePointCount;

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
