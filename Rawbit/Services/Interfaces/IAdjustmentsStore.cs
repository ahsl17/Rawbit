using System.Threading.Tasks;
using Rawbit.Models;

namespace Rawbit.Services.Interfaces;

public interface IAdjustmentsStore
{
    Task<AdjustmentsState?> LoadAsync(string imagePath);
    Task SaveAsync(string imagePath, AdjustmentsState state);
}
