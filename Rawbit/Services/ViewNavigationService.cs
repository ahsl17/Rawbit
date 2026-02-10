using System.Collections.Generic;
using Rawbit.Services.Interfaces;
using MainWindowViewModel = Rawbit.UI.ViewModels.MainWindowViewModel;

namespace Rawbit.Services;

public class ViewNavigationService : IViewNavigationService
{
    public MainWindowViewModel MainWindowViewModel { get; set; }

    public void NavigateToAdjustments(List<ThumnailWithPath> thumbnails)
    {
        MainWindowViewModel.SwitchToAdjustmentViews(thumbnails);
    }
}