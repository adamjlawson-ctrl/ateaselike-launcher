using AtEase.App.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtEase.App.ViewModels;

public partial class DesktopBrowserViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool isEnabled;

    [ObservableProperty]
    private string rootFolder = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Browsing mode is not configured yet.";

    public DesktopBrowserViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var settings = await _settingsService.LoadAsync();
        IsEnabled = settings.DesktopBrowsing.IsEnabled;
        RootFolder = settings.DesktopBrowsing.RootFolder;
        StatusMessage = IsEnabled
            ? $"Browsing root: {RootFolder}"
            : "Browsing mode disabled in settings.";
    }
}
