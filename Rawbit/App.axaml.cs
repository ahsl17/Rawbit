    using Avalonia;
    using Avalonia.Controls.ApplicationLifetimes;
    using Avalonia.Data.Core;
    using Avalonia.Data.Core.Plugins;
    using System.Linq;
    using Avalonia.Markup.Xaml;
    using Microsoft.Extensions.DependencyInjection;
    using Rawbit.Services;
    using Rawbit.Services.Interfaces;
    using MainWindow = Rawbit.UI.Views.MainWindow;
    using MainWindowViewModel = Rawbit.UI.ViewModels.MainWindowViewModel;

    namespace Rawbit;

    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            BindingPlugins.DataValidators.RemoveAt(0);

            var collection = new ServiceCollection();
            collection.AddCommonServices();
            
            var services = collection.BuildServiceProvider();
            
            var vm = services.GetRequiredService<MainWindowViewModel>();
            var navigationService = services.GetRequiredService<IViewNavigationService>();
            ((ViewNavigationService)navigationService).MainWindowViewModel = vm;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // todo proper DI and navigation
                desktop.MainWindow = new MainWindow(vm);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }