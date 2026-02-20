using Microsoft.Extensions.DependencyInjection;
using Rawbit.Data.DbContext;
using Rawbit.Data.Repositories;
using Rawbit.Data.Repositories.Interfaces;
using Rawbit.Services;
using Rawbit.Services.Interfaces;
using AdjustmentsViewModel = Rawbit.UI.Adjustments.ViewModels.AdjustmentsViewModel;
using MainWindowViewModel = Rawbit.UI.Root.MainWindowViewModel;
using ProjectSelectionViewModel = Rawbit.UI.ProjectSelection.ProjectSelectionViewModel;

namespace Rawbit;

public static class ServiceCollectionExtensions
{
      public static void AddCommonServices(this IServiceCollection collection)
        {
            collection.AddSingleton<IProjectDbPathProvider, ProjectDbPathProvider>();
            collection.AddDbContext<RawbitProjectContext>();
            
            collection.AddScoped<ILocalAppStateRepository, LocalAppStateRepository>();
            collection.AddScoped<IImageRepository, ImageRepository>();
            collection.AddSingleton<IViewNavigationService, ViewNavigationService>();
            collection.AddSingleton<IProjectLoaderService, ProjectLoaderService>();
            collection.AddSingleton<IRawLoaderService, RawLoaderService>();
            collection.AddSingleton<IAdjustmentsStore, AdjustmentsStore>();
            collection.AddTransient<AdjustmentsViewModel>();
            collection.AddTransient<ProjectSelectionViewModel>();
            collection.AddTransient<MainWindowViewModel>();
        }
}
