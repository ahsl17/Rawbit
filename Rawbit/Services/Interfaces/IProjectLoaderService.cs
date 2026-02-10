using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Rawbit.Services.Interfaces;

public interface IProjectLoaderService
{
    List<ThumnailWithPath> LoadThumbnailsFromFolder(string folderPath);
    Task RegisterImagesAsync(List<string> select);
}