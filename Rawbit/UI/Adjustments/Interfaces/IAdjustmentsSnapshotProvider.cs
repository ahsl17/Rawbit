using Rawbit.Models;

namespace Rawbit.UI.Adjustments.Interfaces;

public interface IAdjustmentsSnapshotProvider
{
    AdjustmentsState? GetCurrentAdjustmentsStateSnapshot();
    RawImageContainer? GetCurrentImageContainer();
    string? GetSelectedImagePath();
}
