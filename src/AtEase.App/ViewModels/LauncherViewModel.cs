using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using AtEase.App.Models;
using AtEase.App.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AtEase.App.ViewModels;

public class LauncherViewModel : ViewModelBase
{
    private const int AppsTabIndex = 0;
    private const int FoldersTabIndex = 1;

    private readonly SettingsService _settingsService;
    private readonly AppLaunchService _appLaunchService;
    private readonly FolderOpenService _folderOpenService;
    private readonly WallpaperService _wallpaperService;
    private readonly RemovableMediaService _removableMediaService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatcherQueueTimer? _clockTimer;
    private string _statusMessage = "Select an app or folder tile.";
    private string _clockText = string.Empty;
    private int _selectedTabIndex;
    private bool _suppressTabSelectionPersistence;
    private ImageSource? _wallpaperImageSource;

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

    public ImageSource? WallpaperImageSource
    {
        get => _wallpaperImageSource;
        private set
        {
            if (!SetProperty(ref _wallpaperImageSource, value))
            {
                return;
            }

            OnPropertyChanged(nameof(WallpaperVisibility));
            OnPropertyChanged(nameof(FallbackBackgroundVisibility));
        }
    }

    public Visibility WallpaperVisibility => WallpaperImageSource is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility FallbackBackgroundVisibility => WallpaperImageSource is null ? Visibility.Visible : Visibility.Collapsed;

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (!SetProperty(ref _selectedTabIndex, value))
            {
                return;
            }

            if (_suppressTabSelectionPersistence)
            {
                OnPropertyChanged(nameof(ActivePanelLabel));
                OnPropertyChanged(nameof(AppsPanelVisibility));
                OnPropertyChanged(nameof(FoldersPanelVisibility));
                OnPropertyChanged(nameof(AppsActiveTabVisibility));
                OnPropertyChanged(nameof(AppsInactiveTabVisibility));
                OnPropertyChanged(nameof(FoldersActiveTabVisibility));
                OnPropertyChanged(nameof(FoldersInactiveTabVisibility));
                return;
            }

            OnPropertyChanged(nameof(ActivePanelLabel));
            OnPropertyChanged(nameof(AppsPanelVisibility));
            OnPropertyChanged(nameof(FoldersPanelVisibility));
            OnPropertyChanged(nameof(AppsActiveTabVisibility));
            OnPropertyChanged(nameof(AppsInactiveTabVisibility));
            OnPropertyChanged(nameof(FoldersActiveTabVisibility));
            OnPropertyChanged(nameof(FoldersInactiveTabVisibility));
            _ = PersistSelectedSectionAsync(value);
        }
    }

    public string ActivePanelLabel => SelectedTabIndex == FoldersTabIndex ? "Folders" : "Apps";

    public Visibility AppsPanelVisibility => SelectedTabIndex == AppsTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersPanelVisibility => SelectedTabIndex == FoldersTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsActiveTabVisibility => SelectedTabIndex == AppsTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsInactiveTabVisibility => SelectedTabIndex == AppsTabIndex ? Visibility.Collapsed : Visibility.Visible;

    public Visibility FoldersActiveTabVisibility => SelectedTabIndex == FoldersTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersInactiveTabVisibility => SelectedTabIndex == FoldersTabIndex ? Visibility.Collapsed : Visibility.Visible;

    public string ClockText
    {
        get => _clockText;
        private set => SetProperty(ref _clockText, value);
    }

    public IRelayCommand<AppItem> AppTileClickCommand { get; }

    public IRelayCommand<FolderItem> FolderTileClickCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IRelayCommand OpenSettingsFromMenuCommand { get; }

    public IRelayCommand QuitCommand { get; }

    public IRelayCommand RefreshLauncherViewCommand { get; }

    public IRelayCommand RefreshBackgroundCommand { get; }

    public IRelayCommand<string> SwitchPanelCommand { get; }

    public IRelayCommand SleepCommand { get; }

    public IRelayCommand ShutDownCommand { get; }

    public IRelayCommand ExitAtEaseCommand { get; }

    public IRelayCommand EjectRemovableMediaCommand { get; }

    public LauncherViewModel(
        SettingsService settingsService,
        AppLaunchService appLaunchService,
        FolderOpenService folderOpenService,
        WallpaperService wallpaperService,
        RemovableMediaService removableMediaService,
        IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _appLaunchService = appLaunchService;
        _folderOpenService = folderOpenService;
        _wallpaperService = wallpaperService;
        _removableMediaService = removableMediaService;
        _serviceProvider = serviceProvider;
        _settingsService.SettingsSaved += OnSettingsSaved;
        Apps.CollectionChanged += OnTileCollectionsChanged;
        Folders.CollectionChanged += OnTileCollectionsChanged;
        AppTileClickCommand = new RelayCommand<AppItem>(OnAppTileClick);
        FolderTileClickCommand = new RelayCommand<FolderItem>(OnFolderTileClick);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenSettingsFromMenuCommand = new RelayCommand(OpenSettings);
        QuitCommand = new RelayCommand(QuitApplication);
        RefreshLauncherViewCommand = new RelayCommand(RefreshLauncherView);
        RefreshBackgroundCommand = new RelayCommand(RefreshBackground);
        SwitchPanelCommand = new RelayCommand<string>(SwitchPanel);
        SleepCommand = new RelayCommand(HandleSleepAction);
        ShutDownCommand = new RelayCommand(HandleShutDownAction);
        ExitAtEaseCommand = new RelayCommand(QuitApplication);
        EjectRemovableMediaCommand = new RelayCommand(HandleEjectRemovableMediaAction);

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is not null)
        {
            _clockTimer = dispatcherQueue.CreateTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (_, _) => UpdateClock();
            _clockTimer.Start();
        }

        UpdateClock();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        LoadWallpaperBackground();

        var settings = await _settingsService.LoadSettingsAsync();

        _suppressTabSelectionPersistence = true;
        SelectedTabIndex = ToTabIndex(settings.SelectedLauncherSection);
        _suppressTabSelectionPersistence = false;

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

    private void SwitchPanel(string? panel)
    {
        SelectedTabIndex = string.Equals(panel, ProfileSettings.LauncherSectionFolders, StringComparison.OrdinalIgnoreCase)
            ? FoldersTabIndex
            : AppsTabIndex;
    }

    private void RefreshLauncherView()
    {
        _ = InitializeAsync();
        StatusMessage = "Launcher view refreshed.";
    }

    private void RefreshBackground()
    {
        LoadWallpaperBackground();
        StatusMessage = WallpaperImageSource is null
            ? "Using fallback background."
            : "Wallpaper background refreshed.";
    }

    private void HandleSleepAction()
    {
        StatusMessage = "Sleep is not enabled in this phase. Action is prepared with safe fallback.";
    }

    private void HandleShutDownAction()
    {
        StatusMessage = "Shut Down is not enabled in this phase. Action is prepared with safe fallback.";
    }

    private void HandleEjectRemovableMediaAction()
    {
        var devices = _removableMediaService.GetRemovableMediaDisplayNames();
        if (devices.Count == 0)
        {
            StatusMessage = "No removable media found.";
            return;
        }

        StatusMessage = $"Removable media detected: {string.Join(", ", devices)}. Eject flow is prepared for a future phase.";
    }

    private static void QuitApplication()
    {
        Application.Current.Exit();
    }

    private async Task PersistSelectedSectionAsync(int selectedTabIndex)
    {
        try
        {
            var section = selectedTabIndex == FoldersTabIndex
                ? ProfileSettings.LauncherSectionFolders
                : ProfileSettings.LauncherSectionApps;

            await _settingsService.SaveSelectedLauncherSectionAsync(section);
        }
        catch
        {
            // Keep tab switching responsive even if persistence fails.
        }
    }

    private static int ToTabIndex(string? selectedSection)
    {
        return string.Equals(selectedSection, ProfileSettings.LauncherSectionFolders, StringComparison.OrdinalIgnoreCase)
            ? FoldersTabIndex
            : AppsTabIndex;
    }

    private void LoadWallpaperBackground()
    {
        var wallpaperPath = _wallpaperService.GetWallpaperPath();
        if (string.IsNullOrWhiteSpace(wallpaperPath))
        {
            WallpaperImageSource = null;
            return;
        }

        try
        {
            WallpaperImageSource = new BitmapImage(new Uri(wallpaperPath));
        }
        catch
        {
            WallpaperImageSource = null;
        }
    }

    private void UpdateClock()
    {
        ClockText = DateTime.Now.ToString("h:mm tt");
    }
}
