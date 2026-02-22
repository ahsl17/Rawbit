using Avalonia.Controls;

namespace Rawbit.UI.Root.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}
