using Avalonia.Controls;
using Rawbit.UI.ViewModels;

namespace Rawbit.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
