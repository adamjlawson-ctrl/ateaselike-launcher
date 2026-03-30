using AtEase.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace AtEase.App;

public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        InitializeComponent();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        _services = serviceCollection.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow(
            _services.GetRequiredService<LauncherViewModel>(),
            _services.GetRequiredService<Services.ApplicationWindowSwitchService>(),
            _services.GetRequiredService<Services.WindowHandleService>());
        window.Activate();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Services.WindowHandleService>();
        services.AddSingleton<Services.SettingsService>();
        services.AddSingleton<Services.AppLaunchService>();
        services.AddSingleton<Services.FolderOpenService>();
        services.AddSingleton<Services.PathPickerService>();
        services.AddSingleton<Services.CuratedAppDiscoveryService>();
        services.AddSingleton<Services.AppPickerService>();
        services.AddSingleton<Services.WallpaperService>();
        services.AddSingleton<Services.RemovableMediaService>();
        services.AddSingleton<Services.SpecialMenuActionService>();
        services.AddSingleton<Services.ApplicationWindowSwitchService>();
        services.AddSingleton<Services.DisplayLayoutService>();
        services.AddSingleton<Services.ExecutableIconCacheService>();

        services.AddTransient<TileSettingsViewModel>();
        services.AddTransient<SettingsWindow>();

        services.AddSingleton<LauncherViewModel>();
    }
}
