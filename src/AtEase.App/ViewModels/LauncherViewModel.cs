using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using AtEase.App.Models;
using AtEase.App.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace AtEase.App.ViewModels;

public class LauncherViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly AppLaunchService _appLaunchService;
    private readonly FolderOpenService _folderOpenService;
    private readonly IServiceProvider _serviceProvider;
    private string _statusMessage = "Select an app or folder tile.";

    public ObservableCollection<AppItem> Apps { get; } = [];

    public ObservableCollection<FolderItem> Folders { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public Visibility AppsGridVisibility => Apps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsEmptyVisibility => Apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersGridVisibility => Folders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersEmptyVisibility => Folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public IRelayCommand<AppItem> AppTileClickCommand { get; }

    public IRelayCommand<FolderItem> FolderTileClickCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public LauncherViewModel(
        SettingsService settingsService,
        AppLaunchService appLaunchService,
        FolderOpenService folderOpenService,
        IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _appLaunchService = appLaunchService;
        _folderOpenService = folderOpenService;
        _serviceProvider = serviceProvider;
        _settingsService.SettingsSaved += OnSettingsSaved;
        Apps.CollectionChanged += OnTileCollectionsChanged;
        Folders.CollectionChanged += OnTileCollectionsChanged;
        AppTileClickCommand = new RelayCommand<AppItem>(OnAppTileClick);
        FolderTileClickCommand = new RelayCommand<FolderItem>(OnFolderTileClick);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settings = await _settingsService.LoadSettingsAsync();

        Apps.Clear();
        foreach (var app in settings.AppTiles
                     .Where(a => a.IsVisible)
                     .OrderBy(a => a.SortOrder))
        {
            Apps.Add(app);
        }

        Folders.Clear();
        if (settings.ShowFolders)
        {
            foreach (var folder in settings.FolderTiles
                         .Where(f => f.IsVisible)
                         .OrderBy(f => f.SortOrder))
            {
                Folders.Add(folder);
            }
        }

        if (Apps.Count == 0 && Folders.Count == 0)
        {
            StatusMessage = "No launcher tiles are currently configured.";
        }
    }

    private void OnAppTileClick(AppItem? app)
    {
        var result = _appLaunchService.Launch(app);
        StatusMessage = result.Message;
    }

    private void OnFolderTileClick(FolderItem? folder)
    {
        var result = _folderOpenService.Open(folder);
        StatusMessage = result.Message;
    }

    private void OpenSettings()
    {
        try
        {
            var window = _serviceProvider.GetRequiredService<SettingsWindow>();
            window.Activate();
        }
        catch
        {
            StatusMessage = "Could not open settings.";
        }
    }

    private void OnTileCollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AppsGridVisibility));
        OnPropertyChanged(nameof(AppsEmptyVisibility));
        OnPropertyChanged(nameof(FoldersGridVisibility));
        OnPropertyChanged(nameof(FoldersEmptyVisibility));
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        _ = InitializeAsync();
        StatusMessage = "Launcher refreshed from saved settings.";
    }
}
