using AtEase.App.ViewModels;
using AtEase.App.Services;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AtEase.App;

public sealed partial class SettingsWindow : Window
{
    public TileSettingsViewModel ViewModel { get; }

    public SettingsWindow(TileSettingsViewModel viewModel, WindowHandleService windowHandleService)
    {
        ViewModel = viewModel;
        InitializeComponent();

        windowHandleService.CurrentWindowHandle = WindowNative.GetWindowHandle(this);
        Activated += (_, _) => windowHandleService.CurrentWindowHandle = WindowNative.GetWindowHandle(this);

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }
    }
}