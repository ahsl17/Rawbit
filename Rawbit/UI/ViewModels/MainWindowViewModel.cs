using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Rawbit.Services;
using Rawbit.UI.ViewModels.Interfaces;

namespace Rawbit.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private INavigableViewModel _content;
    public AdjustmentsViewModel AdjustmentsViewModel { get; set; }
    public ProjectSelectionViewModel ProjectSelectionViewModel { get; set; }

    public MainWindowViewModel(AdjustmentsViewModel adjustmentsViewModel, ProjectSelectionViewModel projectSelectionViewModel)
    {
        AdjustmentsViewModel = adjustmentsViewModel;
        ProjectSelectionViewModel = projectSelectionViewModel;
        _content = ProjectSelectionViewModel;
        
        WeakReferenceMessenger.Default.Register<INavigableViewModel>(this, (r, viewModel) =>
        {
            Content = viewModel;
        });
    }

    public void SwitchToAdjustmentViews(List<ThumnailWithPath> thumbnails)
    {
        AdjustmentsViewModel.FolderThumbnails = thumbnails;
        Content = AdjustmentsViewModel;
    }
}