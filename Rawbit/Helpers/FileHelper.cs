using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Rawbit.Helpers;

public static class FileHelper
{
    public static IReadOnlyList<string> RawSupportedFileFormats => ["ARW", "CR2", "NEF", "DNG"];

    public static async Task<IReadOnlyList<IStorageFolder>> SelectFolderFromFolderPickerAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return [];

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()).ConfigureAwait(false);
        return folder;
    }

    public static async Task<IReadOnlyList<IStorageFile>> SelectFilesFromFilePickerAsync(string title, string fileTypeInfo, string[] supportedFileFormats)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return [];

        var files = await window.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = title,
                FileTypeFilter =
                [
                    new FilePickerFileType(fileTypeInfo)
                    {
                        Patterns = supportedFileFormats.Select(f => $"*.{f}").ToArray()
                    }
                ]
            }).ConfigureAwait(false);
        return files;
    }

    public static async Task<IStorageFile?> SaveFileAsync(string actionName, string fileType, string extension)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return null;

        var file = await window.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = actionName,
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType(fileType)
                    {
                        Patterns = [$"*.{extension}"]
                    }
                ]
            }).ConfigureAwait(false);
        return file;
    }
}