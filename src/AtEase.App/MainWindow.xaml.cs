using AtEase.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AtEase.App;

public sealed partial class MainWindow : Window
{
    public LauncherViewModel ViewModel { get; }

    public MainWindow(LauncherViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ConfigureImmersivePresentation();

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }
    }

    private void ConfigureImmersivePresentation()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        }
        catch
        {
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
                {
                    overlappedPresenter.Maximize();
                }
            }
            catch
            {
                // Keep the default window mode if immersive presentation is unavailable.
            }
        }
    }
}
