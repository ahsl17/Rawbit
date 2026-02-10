using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rawbit.Data.Repositories.Interfaces;

public interface IImageRepository
{
    Task<List<string>> GetImagesToRegisterAsync(List<string> imagesPath);
    Task RegisterImagesAsync(IEnumerable<string> images);
}