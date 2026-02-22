using System.Threading;
using System.Threading.Tasks;
using Rawbit.Models;

namespace Rawbit.Services.Interfaces;

public interface IExportService
{
    Task ExportJpegAsync(
        RawImageContainer container,
        AdjustmentsState adjustments,
        string destinationFolder,
        string fileName,
        int quality,
        CancellationToken cancellationToken);
}
