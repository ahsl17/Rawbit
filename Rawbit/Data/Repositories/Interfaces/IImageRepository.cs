using System.Collections.Generic;
using System.Threading.Tasks;
using Rawbit.Models;

namespace Rawbit.Data.Repositories.Interfaces;

public interface IImageRepository
{
    Task<List<string>> GetImagesToRegisterAsync(List<string> imagesPath);
    Task RegisterImagesAsync(IEnumerable<string> images);
    Task<Adjustments?> GetAdjustmentsByPathAsync(string imagePath);
    Task UpdateAdjustmentsByPathAsync(string imagePath, Adjustments adjustments);
}
