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
}