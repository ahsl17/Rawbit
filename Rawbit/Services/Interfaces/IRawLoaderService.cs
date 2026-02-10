using System.Threading.Tasks;
using Rawbit.Models;

namespace Rawbit.Services.Interfaces;

public interface IRawLoaderService
{
    Task<RawImageContainer> LoadRawImageAsync(string path);
}