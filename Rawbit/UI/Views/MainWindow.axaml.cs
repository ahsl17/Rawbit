using Avalonia.Controls;
using MainWindowViewModel = Rawbit.UI.Root.MainWindowViewModel;

namespace Rawbit.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
