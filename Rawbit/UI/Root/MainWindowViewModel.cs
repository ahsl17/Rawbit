using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Rawbit.Services;
using Rawbit.UI.Adjustments.ViewModels;
using Rawbit.UI.ProjectSelection;
using Rawbit.UI.Root.Interfaces;

namespace Rawbit.UI.Root;

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