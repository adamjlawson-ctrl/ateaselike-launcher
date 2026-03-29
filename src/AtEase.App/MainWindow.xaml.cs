using AtEase.App.ViewModels;
using AtEase.App.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AtEase.App;

public sealed partial class MainWindow : Window
{
    public LauncherViewModel ViewModel { get; }
    private readonly ApplicationWindowSwitchService _applicationWindowSwitchService;
    private readonly WindowHandleService _windowHandleService;

    public MainWindow(
        LauncherViewModel viewModel,
        ApplicationWindowSwitchService applicationWindowSwitchService,
        WindowHandleService windowHandleService)
    {
        ViewModel = viewModel;
        _applicationWindowSwitchService = applicationWindowSwitchService;
        _windowHandleService = windowHandleService;
        InitializeComponent();
        CaptureLauncherWindowHandle();
        Activated += (_, _) => CaptureLauncherWindowHandle();
        ConfigureImmersivePresentation();

        if (Content is FrameworkElement element)
        {
            element.DataContext = ViewModel;
        }
    }

    private void CaptureLauncherWindowHandle()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        _applicationWindowSwitchService.RegisterLauncherWindow(hwnd);
        _windowHandleService.LauncherWindowHandle = hwnd;
        if (_windowHandleService.CurrentWindowHandle == 0)
        {
            _windowHandleService.CurrentWindowHandle = hwnd;
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
