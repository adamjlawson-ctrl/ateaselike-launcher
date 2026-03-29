using AtEase.App.ViewModels;
using Microsoft.UI.Xaml;

namespace AtEase.App;

public sealed partial class MainWindow : Window
{
    public LauncherViewModel ViewModel { get; }

    public MainWindow(LauncherViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }
    }
}
