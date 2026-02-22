using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rawbit.Helpers;
using Rawbit.Services.Interfaces;
using Rawbit.UI.Adjustments.Interfaces;

namespace Rawbit.UI.Adjustments.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly IExportService _exportService;
    private readonly IAdjustmentsSnapshotProvider _snapshotProvider;

    [ObservableProperty] private string _exportDestination = string.Empty;
    [ObservableProperty] private int _exportQuality = 90;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private double _exportProgress;

    public ExportViewModel(IExportService exportService, IAdjustmentsSnapshotProvider snapshotProvider)
    {
        _exportService = exportService;
        _snapshotProvider = snapshotProvider;
    }

    public string ExportDestinationDisplay =>
        string.IsNullOrWhiteSpace(ExportDestination) ? "Not selected" : ExportDestination;

    partial void OnExportDestinationChanged(string value) =>
        OnPropertyChanged(nameof(ExportDestinationDisplay));

    [RelayCommand]
    private async Task SelectFolder()
    {
        var selection = await FileHelper.SelectFolderFromFolderPickerAsync();
        var folder = selection.FirstOrDefault();
        if (folder is null)
            return;

        ExportDestination = folder.Path.LocalPath;
    }

    [RelayCommand]
    private async Task ExportToJpgAsync()
    {
        if (IsExporting)
            return;

        var sourcePath = _snapshotProvider.GetSelectedImagePath();
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        if (string.IsNullOrWhiteSpace(ExportDestination))
            return;

        using var source = _snapshotProvider.GetFullResImage();
        if (source == null)
            return;

        var state = _snapshotProvider.GetCurrentAdjustmentsStateSnapshot();
        if (state == null)
            return;

        IsExporting = true;
        ExportProgress = 0;
        try
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(sourcePath)}.jpg";
            var progress = new Progress<double>(value => ExportProgress = value);
            await _exportService.ExportJpegAsync(
                source,
                state,
                ExportDestination,
                fileName,
                ExportQuality,
                CancellationToken.None,
                progress).ConfigureAwait(false);
        }
        finally
        {
            Dispatcher.UIThread.Post(() => IsExporting = false);
            Dispatcher.UIThread.Post(() => ExportProgress = 0);
        }
    }
}
