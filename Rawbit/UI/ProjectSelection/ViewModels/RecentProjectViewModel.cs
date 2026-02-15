using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rawbit.Data.ApplicationState;
using Rawbit.Services.Interfaces;

namespace Rawbit.UI.ProjectSelection.ViewModels;

public partial class RecentProjectViewModel : ObservableObject
{
    private readonly Func<string, string, Task> _onOpen;

    [ObservableProperty] private Project _project;

    public RecentProjectViewModel(Project project, Func<string, string, Task> onOpen)
    {
        _project = project;
        _onOpen = onOpen;
    }

    [RelayCommand]
    public async Task OpenAsync()
    {
        await _onOpen.Invoke(Project.Path, $"{Project.Name}.rbproj");
    }
}