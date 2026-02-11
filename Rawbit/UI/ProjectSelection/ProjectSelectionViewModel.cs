using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rawbit.Data.DbContext;
using Rawbit.Helpers;
using Rawbit.Services.Interfaces;
using Rawbit.UI.Root.Interfaces;

namespace Rawbit.UI.ProjectSelection;

public partial class ProjectSelectionViewModel : INavigableViewModel
{
    private IRawLoaderService _rawLoaderService;
    private readonly IProjectLoaderService _projectLoaderService;
    private readonly IViewNavigationService _viewNavigationService;
    private readonly IProjectDbPathProvider _projectDbPathProvider;
    private readonly IServiceProvider _serviceProvider;

    public ProjectSelectionViewModel(IRawLoaderService rawLoaderService, 
        IProjectLoaderService projectLoaderService,
        IViewNavigationService viewNavigationService,
        IProjectDbPathProvider projectDbPathProvider,
        IServiceProvider serviceProvider)
    {
        _rawLoaderService = rawLoaderService;
        _projectLoaderService = projectLoaderService;
        _viewNavigationService = viewNavigationService;
        _projectDbPathProvider = projectDbPathProvider;
        _serviceProvider = serviceProvider;
    }

    [RelayCommand]
    public async Task CreateNewProjectAsync()
    {
        var file = await FileHelper.SaveFileAsync("Create a new project", "Rawbit Project", "rbproj").ConfigureAwait(false);
        if (file == null) return;
        File.Create(file.Path.AbsolutePath);
        var directoryPath = Path.GetDirectoryName(file.Path.AbsolutePath);
        if (directoryPath is null) return;
        var rbDataFolderName = file.Name.Replace(".rbproj", "") + ".rbdata";
        Directory.CreateDirectory(Path.Combine(directoryPath, rbDataFolderName));
        await InitDbAsync(Path.Combine(directoryPath, rbDataFolderName), file.Name).ConfigureAwait(false);
        var thumbnailsFromFolder = _projectLoaderService.LoadThumbnailsFromFolder(directoryPath);
        await _projectLoaderService.RegisterImagesAsync(thumbnailsFromFolder.Select(t => t.Path).ToList()).ConfigureAwait(false);
        _viewNavigationService.NavigateToAdjustments(thumbnailsFromFolder);
    }

    [RelayCommand]
    public async Task OpenExistingProjectAsync()
    {
        var files = await FileHelper.SelectFilesFromFilePickerAsync("Select a Rawbit project", "Rawbit Project", new[] { "rbproj" }).ConfigureAwait(false);
        if (files.Count == 0) return;
        var directoryPath = Path.GetDirectoryName(files[0].Path.AbsolutePath);
        if (directoryPath is null) return;
        var rbDataFolderName = files[0].Name.Replace(".rbproj", "") + ".rbdata";

        await InitDbAsync(Path.Combine(directoryPath, rbDataFolderName), files[0].Name).ConfigureAwait(false);
        _viewNavigationService.NavigateToAdjustments(_projectLoaderService.LoadThumbnailsFromFolder(directoryPath));
    }

    private async Task InitDbAsync(string path, string name)
    {
        _projectDbPathProvider.DbPath = Path.Combine(path, $"{name}.db");
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RawbitProjectContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
}