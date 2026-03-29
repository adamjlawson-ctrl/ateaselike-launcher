using AtEase.App.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace AtEase.App.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public HomeViewModel Home { get; }

    public SettingsViewModel Settings { get; }

    public DesktopBrowserViewModel Browser { get; }

    [ObservableProperty]
    private string currentPage = "Home";

    public Visibility HomeVisibility => CurrentPage == "Home" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SettingsVisibility => CurrentPage == "Settings" ? Visibility.Visible : Visibility.Collapsed;

    public Visibility BrowserVisibility => CurrentPage == "Browse" ? Visibility.Visible : Visibility.Collapsed;

    public ShellViewModel(
        HomeViewModel home,
        SettingsViewModel settings,
        DesktopBrowserViewModel browser,
        INavigationService navigationService)
    {
        Home = home;
        Settings = settings;
        Browser = browser;
        _navigationService = navigationService;

        _navigationService.PageChanged += OnPageChanged;
        _navigationService.NavigateTo("Home");

        _ = InitializeAsync();
    }

    [RelayCommand]
    public void ShowHome() => _navigationService.NavigateTo("Home");

    [RelayCommand]
    public void ShowSettings() => _navigationService.NavigateTo("Settings");

    [RelayCommand]
    public void ShowBrowser() => _navigationService.NavigateTo("Browse");

    private async Task InitializeAsync()
    {
        await Home.RefreshAsync();
        await Settings.LoadAsync();
        await Browser.RefreshAsync();
    }

    partial void OnCurrentPageChanged(string value)
    {
        OnPropertyChanged(nameof(HomeVisibility));
        OnPropertyChanged(nameof(SettingsVisibility));
        OnPropertyChanged(nameof(BrowserVisibility));
    }

    private void OnPageChanged(object? sender, EventArgs e)
    {
        CurrentPage = _navigationService.CurrentPageKey;
    }
}
