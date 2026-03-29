using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
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
    private const string AtEaseApplicationLabel = "At Ease";

    private readonly SettingsService _settingsService;
    private readonly AppLaunchService _appLaunchService;
    private readonly FolderOpenService _folderOpenService;
    private readonly WallpaperService _wallpaperService;
    private readonly RemovableMediaService _removableMediaService;
    private readonly DisplayLayoutService _displayLayoutService;
    private readonly SpecialMenuActionService _specialMenuActionService;
    private readonly ApplicationWindowSwitchService _applicationWindowSwitchService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DispatcherQueueTimer? _clockTimer;
    private readonly Stack<string> _folderNavigationBackStack = [];
    private readonly List<FolderItem> _configuredFolders = [];
    private readonly int _launcherProcessId = Environment.ProcessId;
    private string _statusMessage = "Select an app or folder tile.";
    private string _clockText = string.Empty;
    private int _selectedTabIndex;
    private string? _selectedMediaPanelId;
    private RemovableMediaPanel? _folderContentPanel;
    private bool _isFolderContentPanelActive;
    private bool _isAppActiveMode;
    private bool _suppressTabSelectionPersistence;
    private ImageSource? _wallpaperImageSource;
    private string _currentApplicationDisplayName = AtEaseApplicationLabel;

    public ObservableCollection<AppItem> Apps { get; } = [];

    public ObservableCollection<FolderItem> Folders { get; } = [];

    public ObservableCollection<RemovableMediaPanel> RemovableMediaPanels { get; } = [];

    public ObservableCollection<MediaPanelItem> MediaItems { get; } = [];

    public ObservableCollection<MediaPanelItem> FolderBrowserItems { get; } = [];

    public ObservableCollection<TrackedApplication> TrackedApplications { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string CurrentApplicationDisplayName
    {
        get => _currentApplicationDisplayName;
        private set => SetProperty(ref _currentApplicationDisplayName, value);
    }

    public bool IsAppActiveMode
    {
        get => _isAppActiveMode;
        private set
        {
            if (!SetProperty(ref _isAppActiveMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(LauncherMenuVisibility));
            OnPropertyChanged(nameof(LauncherContentVisibility));
            OnPropertyChanged(nameof(ReturnToAtEaseButtonVisibility));
        }
    }

    public Visibility AppsGridVisibility => Apps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsEmptyVisibility => Apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersGridVisibility => !_isFolderContentPanelActive && Folders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersEmptyVisibility => !_isFolderContentPanelActive && Folders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FolderBrowserGridVisibility => _isFolderContentPanelActive && FolderBrowserItems.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility FolderBrowserEmptyVisibility => _isFolderContentPanelActive && FolderBrowserItems.Count == 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility RemovableMediaTabsVisibility => RemovableMediaPanels.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility LauncherMenuVisibility => IsAppActiveMode ? Visibility.Collapsed : Visibility.Visible;

    public Visibility LauncherContentVisibility => IsAppActiveMode ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ReturnToAtEaseButtonVisibility => IsAppActiveMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FolderBackButtonVisibility => _isFolderContentPanelActive
        ? Visibility.Visible
        : Visibility.Collapsed;

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
                UpdatePanelState();
                return;
            }

            UpdatePanelState();
            _ = PersistSelectedSectionAsync(value);
        }
    }

    public string ActivePanelLabel
    {
        get
        {
            if (SelectedMediaPanel is not null)
            {
                return SelectedMediaPanel.Title;
            }

            if (_isFolderContentPanelActive && _folderContentPanel is not null)
            {
                return _folderContentPanel.Title;
            }

            return SelectedTabIndex == FoldersTabIndex ? "Folders" : "Apps";
        }
    }

    public Visibility AppsPanelVisibility => SelectedMediaPanel is null && SelectedTabIndex == AppsTabIndex
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility FoldersPanelVisibility => SelectedMediaPanel is null && SelectedTabIndex == FoldersTabIndex
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility MediaPanelVisibility => SelectedMediaPanel is null ? Visibility.Collapsed : Visibility.Visible;

    public Visibility MediaItemsEmptyVisibility => MediaItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsActiveTabVisibility => SelectedTabIndex == AppsTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AppsInactiveTabVisibility => SelectedTabIndex != AppsTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersActiveTabVisibility => SelectedTabIndex == FoldersTabIndex ? Visibility.Visible : Visibility.Collapsed;

    public Visibility FoldersInactiveTabVisibility => SelectedTabIndex != FoldersTabIndex ? Visibility.Visible : Visibility.Collapsed;

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

    public IRelayCommand<string> OpenMediaPanelCommand { get; }

    public IRelayCommand NavigateFolderBackCommand { get; }

    public IRelayCommand ReturnToAtEaseCommand { get; }

    public IRelayCommand<MediaPanelItem> MediaItemClickCommand { get; }

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
        DisplayLayoutService displayLayoutService,
        SpecialMenuActionService specialMenuActionService,
        ApplicationWindowSwitchService applicationWindowSwitchService,
        IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _appLaunchService = appLaunchService;
        _folderOpenService = folderOpenService;
        _wallpaperService = wallpaperService;
        _removableMediaService = removableMediaService;
        _displayLayoutService = displayLayoutService;
        _specialMenuActionService = specialMenuActionService;
        _applicationWindowSwitchService = applicationWindowSwitchService;
        _serviceProvider = serviceProvider;
        _settingsService.SettingsSaved += OnSettingsSaved;
        _appLaunchService.ApplicationLaunched += OnApplicationLaunched;
        Apps.CollectionChanged += OnTileCollectionsChanged;
        Folders.CollectionChanged += OnTileCollectionsChanged;
        MediaItems.CollectionChanged += OnTileCollectionsChanged;
        FolderBrowserItems.CollectionChanged += OnTileCollectionsChanged;
        AppTileClickCommand = new RelayCommand<AppItem>(OnAppTileClick);
        FolderTileClickCommand = new RelayCommand<FolderItem>(OnFolderTileClick);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenSettingsFromMenuCommand = new RelayCommand(OpenSettings);
        QuitCommand = new RelayCommand(QuitApplication);
        RefreshLauncherViewCommand = new RelayCommand(RefreshLauncherView);
        RefreshBackgroundCommand = new RelayCommand(RefreshBackground);
        SwitchPanelCommand = new RelayCommand<string>(SwitchPanel);
        OpenMediaPanelCommand = new RelayCommand<string>(OpenMediaPanel);
        NavigateFolderBackCommand = new RelayCommand(NavigateFolderBack);
        ReturnToAtEaseCommand = new RelayCommand(SwitchToAtEase);
        MediaItemClickCommand = new RelayCommand<MediaPanelItem>(OnMediaItemClick);
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

        _configuredFolders.Clear();
        if (settings.ShowFolders)
        {
            foreach (var folder in settings.FolderTiles
                         .Where(f => f.IsVisible)
                         .OrderBy(f => f.SortOrder))
            {
                _configuredFolders.Add(folder);
            }
        }

        RefreshFolderRootItems();

        RefreshRemovableMediaPanels();

        ApplySelectedSection(settings.SelectedLauncherSection);

        if (SelectedMediaPanel is not null)
        {
            LoadMediaItems(SelectedMediaPanel);
        }

        if (Apps.Count == 0 && Folders.Count == 0)
        {
            StatusMessage = "No launcher tiles are currently configured.";
        }

        RefreshApplicationTrackingState();
    }

    private void OnAppTileClick(AppItem? app)
    {
        var result = _appLaunchService.Launch(app, null);
        StatusMessage = result.Message;
        RefreshApplicationTrackingState();
    }

    public IReadOnlyList<DisplayTarget> GetAvailableDisplays()
    {
        return _displayLayoutService.GetDisplays();
    }

    public void LaunchAppOnDisplay(AppItem? app, DisplayTarget? displayTarget)
    {
        var result = _appLaunchService.Launch(app, displayTarget);
        StatusMessage = result.Message;
        RefreshApplicationTrackingState();
    }

    private void OnFolderTileClick(FolderItem? folder)
    {
        if (folder is null)
        {
            StatusMessage = "The selected folder is not available.";
            return;
        }

        var resolvedPath = ResolveDirectoryPath(folder.Path);
        if (resolvedPath is null)
        {
            StatusMessage = $"{folder.DisplayName} cannot be opened because the folder was not found.";
            return;
        }

        OpenFolderContentPanel(resolvedPath, folder.DisplayName);
        StatusMessage = $"Opened folder '{ActivePanelLabel}'.";
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
        OnPropertyChanged(nameof(FolderBrowserGridVisibility));
        OnPropertyChanged(nameof(FolderBrowserEmptyVisibility));
        OnPropertyChanged(nameof(MediaItemsEmptyVisibility));
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        _ = InitializeAsync();
        StatusMessage = "Launcher refreshed from saved settings.";
    }

    private void SwitchPanel(string? panel)
    {
        _selectedMediaPanelId = null;
        ExitFolderBrowsing();
        if (string.Equals(panel, ProfileSettings.LauncherSectionFolders, StringComparison.OrdinalIgnoreCase))
        {
            RefreshFolderRootItems();
        }
        OnPropertyChanged(nameof(FolderBackButtonVisibility));
        SelectedTabIndex = string.Equals(panel, ProfileSettings.LauncherSectionFolders, StringComparison.OrdinalIgnoreCase)
            ? FoldersTabIndex
            : AppsTabIndex;
    }

    private void OpenMediaPanel(string? panelId)
    {
        if (string.IsNullOrWhiteSpace(panelId))
        {
            return;
        }

        _selectedMediaPanelId = panelId;
        ExitFolderBrowsing();
        OnPropertyChanged(nameof(FolderBackButtonVisibility));
        UpdatePanelState();

        if (SelectedMediaPanel is { } panel)
        {
            LoadMediaItems(panel);
            _ = PersistSelectedSectionAsync(panel.Id);
        }
    }

    private void OnMediaItemClick(MediaPanelItem? item)
    {
        if (item is null)
        {
            return;
        }

        var selectedPanel = SelectedContentPanel;
        if (selectedPanel is null)
        {
            return;
        }

        if (item.IsFolder)
        {
            var resolvedPath = ResolveDirectoryPath(item.Path);
            if (resolvedPath is null)
            {
                StatusMessage = $"Could not open folder '{item.DisplayName}'.";
                return;
            }

            if (_isFolderContentPanelActive && !string.Equals(selectedPanel.CurrentPath, resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                _folderNavigationBackStack.Push(selectedPanel.CurrentPath);
                OnPropertyChanged(nameof(FolderBackButtonVisibility));
            }

            selectedPanel.CurrentPath = resolvedPath;

            if (_isFolderContentPanelActive)
            {
                selectedPanel.Title = BuildFolderPanelTitle(resolvedPath, string.Empty);
                LoadFolderBrowserItems(selectedPanel.CurrentPath);
            }
            else
            {
                LoadMediaItems(selectedPanel);
            }

            StatusMessage = item.IsParentNavigation
                ? "Moved to parent folder."
                : $"Opened folder '{item.DisplayName}'.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true
            });
            StatusMessage = $"Opened '{item.DisplayName}'.";
        }
        catch
        {
            StatusMessage = $"Could not open '{item.DisplayName}'.";
        }
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
        var result = _specialMenuActionService.Sleep();
        StatusMessage = result.Message;
    }

    private void HandleShutDownAction()
    {
        var result = _specialMenuActionService.ShutDown();
        StatusMessage = result.Message;
    }

    private void HandleEjectRemovableMediaAction()
    {
        var result = _specialMenuActionService.EjectRemovableMedia();
        StatusMessage = result.Message;
    }

    public void SwitchToAtEase()
    {
        if (_applicationWindowSwitchService.BringLauncherToFront())
        {
            IsAppActiveMode = false;
            RefreshApplicationTrackingState();
            StatusMessage = "Switched to At Ease.";
            return;
        }

        StatusMessage = "Could not bring At Ease to the front.";
    }

    public void SwitchToTrackedApplication(TrackedApplication? application)
    {
        if (application is null)
        {
            return;
        }

        if (_applicationWindowSwitchService.TryBringProcessToFront(application.ProcessId))
        {
            RefreshApplicationTrackingState();
            StatusMessage = $"Switched to {application.DisplayName}.";
            return;
        }

        RemoveTrackedApplicationById(application.ProcessId);
        RefreshApplicationTrackingState();
        StatusMessage = $"{application.DisplayName} is no longer running.";
    }

    public void RefreshApplicationTrackingState()
    {
        CleanupExitedTrackedApplications();

        var foregroundProcessId = _applicationWindowSwitchService.GetForegroundProcessId();
        var isLauncherForeground = foregroundProcessId.HasValue && foregroundProcessId.Value == _launcherProcessId;

        var activeTrackedApplication = TryResolveActiveTrackedApplication(foregroundProcessId);

        // Treat any non-launcher foreground while tracked launched apps exist as app-active mode.
        IsAppActiveMode = !isLauncherForeground && TrackedApplications.Count > 0;

        CurrentApplicationDisplayName = activeTrackedApplication?.DisplayName
            ?? (IsAppActiveMode ? "Application" : AtEaseApplicationLabel);
    }

    public void EjectRemovableMediaByPanel(RemovableMediaPanel? panel)
    {
        if (panel is null)
        {
            StatusMessage = "No removable media selected.";
            return;
        }

        var result = _specialMenuActionService.EjectRemovableMedia(panel.RootPath, panel.Title);
        StatusMessage = result.Message;

        RefreshRemovableMediaPanels();
        if (SelectedMediaPanel is { } selectedPanel)
        {
            LoadMediaItems(selectedPanel);
        }
    }

    private void NavigateFolderBack()
    {
        if (!_isFolderContentPanelActive || _folderContentPanel is null)
        {
            return;
        }

        if (_folderNavigationBackStack.Count == 0)
        {
            ExitFolderBrowsing();
            RefreshFolderRootItems();
            UpdatePanelState();
            StatusMessage = "Returned to Folders panel.";
            return;
        }

        _folderContentPanel.CurrentPath = _folderNavigationBackStack.Pop();
        _folderContentPanel.Title = BuildFolderPanelTitle(_folderContentPanel.CurrentPath, string.Empty);
        OnPropertyChanged(nameof(FolderBackButtonVisibility));
        LoadFolderBrowserItems(_folderContentPanel.CurrentPath);
        StatusMessage = "Moved back.";
    }

    private void QuitApplication()
    {
        StatusMessage = _specialMenuActionService.ExitAtEase().Message;
        Application.Current.Exit();
    }

    private async Task PersistSelectedSectionAsync(int selectedTabIndex)
    {
        var section = selectedTabIndex == FoldersTabIndex
            ? ProfileSettings.LauncherSectionFolders
            : ProfileSettings.LauncherSectionApps;

        await PersistSelectedSectionAsync(section);
    }

    private async Task PersistSelectedSectionAsync(string section)
    {
        try
        {
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

    private RemovableMediaPanel? SelectedMediaPanel => RemovableMediaPanels.FirstOrDefault(p => p.Id == _selectedMediaPanelId);

    private RemovableMediaPanel? SelectedContentPanel => _isFolderContentPanelActive ? _folderContentPanel : SelectedMediaPanel;

    private void RefreshRemovableMediaPanels()
    {
        var detectedPanels = _removableMediaService.GetRemovableMediaPanels();

        RemovableMediaPanels.Clear();
        foreach (var panel in detectedPanels)
        {
            RemovableMediaPanels.Add(panel);
        }

        if (_selectedMediaPanelId is not null && RemovableMediaPanels.All(p => p.Id != _selectedMediaPanelId))
        {
            _selectedMediaPanelId = null;
        }

        OnPropertyChanged(nameof(RemovableMediaTabsVisibility));
    }

    private void ApplySelectedSection(string selectedSection)
    {
        ExitFolderBrowsing();
        OnPropertyChanged(nameof(FolderBackButtonVisibility));

        if (!string.IsNullOrWhiteSpace(selectedSection) &&
            selectedSection.StartsWith(ProfileSettings.LauncherSectionMediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (RemovableMediaPanels.Any(p => p.Id == selectedSection))
            {
                _selectedMediaPanelId = selectedSection;
                _suppressTabSelectionPersistence = true;
                SelectedTabIndex = AppsTabIndex;
                _suppressTabSelectionPersistence = false;
                UpdatePanelState();
                return;
            }
        }

        _selectedMediaPanelId = null;
        _suppressTabSelectionPersistence = true;
        SelectedTabIndex = ToTabIndex(selectedSection);
        _suppressTabSelectionPersistence = false;

        if (SelectedTabIndex == FoldersTabIndex)
        {
            RefreshFolderRootItems();
        }

        UpdatePanelState();
    }

    private void LoadMediaItems(RemovableMediaPanel panel)
    {
        MediaItems.Clear();

        var items = _removableMediaService.GetPanelItems(panel.RootPath, panel.CurrentPath);
        foreach (var item in items)
        {
            MediaItems.Add(item);
        }

        if (MediaItems.Count == 0)
        {
            StatusMessage = "No files or folders in this media location.";
        }
    }

    private void LoadFolderBrowserItems(string currentPath)
    {
        if (!Directory.Exists(currentPath))
        {
            ExitFolderBrowsing();
            RefreshFolderRootItems();
            UpdatePanelState();
            StatusMessage = "This folder or drive is no longer available.";
            return;
        }

        FolderBrowserItems.Clear();
        var items = _removableMediaService.GetPanelItems(currentPath, currentPath);
        foreach (var item in items)
        {
            FolderBrowserItems.Add(item);
        }

        OnPropertyChanged(nameof(FolderBrowserGridVisibility));
        OnPropertyChanged(nameof(FolderBrowserEmptyVisibility));

        if (FolderBrowserItems.Count == 0)
        {
            StatusMessage = "No visible files or folders in this folder.";
        }
    }

    private void UpdatePanelState()
    {
        foreach (var panel in RemovableMediaPanels)
        {
            panel.IsActive = panel.Id == _selectedMediaPanelId;
        }

        if (_folderContentPanel is not null)
        {
            _folderContentPanel.IsActive = _isFolderContentPanelActive;
        }

        OnPropertyChanged(nameof(ActivePanelLabel));
        OnPropertyChanged(nameof(AppsPanelVisibility));
        OnPropertyChanged(nameof(FoldersPanelVisibility));
        OnPropertyChanged(nameof(MediaPanelVisibility));
        OnPropertyChanged(nameof(FoldersGridVisibility));
        OnPropertyChanged(nameof(FoldersEmptyVisibility));
        OnPropertyChanged(nameof(FolderBrowserGridVisibility));
        OnPropertyChanged(nameof(FolderBrowserEmptyVisibility));
        OnPropertyChanged(nameof(FolderBackButtonVisibility));
        OnPropertyChanged(nameof(AppsActiveTabVisibility));
        OnPropertyChanged(nameof(AppsInactiveTabVisibility));
        OnPropertyChanged(nameof(FoldersActiveTabVisibility));
        OnPropertyChanged(nameof(FoldersInactiveTabVisibility));
    }

    private void OpenFolderContentPanel(string folderPath, string folderDisplayName)
    {
        var title = BuildFolderPanelTitle(folderPath, folderDisplayName);

        _folderNavigationBackStack.Clear();
        OnPropertyChanged(nameof(FolderBackButtonVisibility));

        _folderContentPanel = new RemovableMediaPanel
        {
            Id = $"folder:{folderPath.ToLowerInvariant()}",
            Title = title,
            RootPath = folderPath,
            CurrentPath = folderPath,
            IsActive = true
        };

        _selectedMediaPanelId = null;
        _isFolderContentPanelActive = true;
        UpdatePanelState();
        LoadFolderBrowserItems(_folderContentPanel.CurrentPath);
    }

    private void ExitFolderBrowsing()
    {
        _isFolderContentPanelActive = false;
        _folderNavigationBackStack.Clear();
        _folderContentPanel = null;
        FolderBrowserItems.Clear();
    }

    private void RefreshFolderRootItems()
    {
        Folders.Clear();

        foreach (var folder in _configuredFolders)
        {
            Folders.Add(folder);
        }

        OnPropertyChanged(nameof(FoldersGridVisibility));
        OnPropertyChanged(nameof(FoldersEmptyVisibility));
    }

    private static string? ResolveDirectoryPath(string? rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(rawPath.Trim());
        var unquoted = expanded.Trim().Trim('"');
        if (!Directory.Exists(unquoted))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(unquoted);
        }
        catch
        {
            return unquoted;
        }
    }

    private static string BuildFolderPanelTitle(string folderPath, string folderDisplayName)
    {
        if (!string.IsNullOrWhiteSpace(folderDisplayName))
        {
            return folderDisplayName;
        }

        var trimmed = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return folderPath;
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
        RefreshApplicationTrackingState();
    }

    private void OnApplicationLaunched(TrackedApplication application)
    {
        RemoveTrackedApplicationById(application.ProcessId);
        TrackedApplications.Add(application);
        RefreshApplicationTrackingState();
    }

    private void CleanupExitedTrackedApplications()
    {
        for (var i = TrackedApplications.Count - 1; i >= 0; i--)
        {
            if (!IsProcessRunning(TrackedApplications[i].ProcessId))
            {
                TrackedApplications.RemoveAt(i);
            }
        }
    }

    private void RemoveTrackedApplicationById(int processId)
    {
        var existing = TrackedApplications.FirstOrDefault(a => a.ProcessId == processId);
        if (existing is not null)
        {
            TrackedApplications.Remove(existing);
        }
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private TrackedApplication? TryResolveActiveTrackedApplication(int? foregroundProcessId)
    {
        if (!foregroundProcessId.HasValue)
        {
            return null;
        }

        var directMatch = TrackedApplications.FirstOrDefault(a => a.ProcessId == foregroundProcessId.Value);
        if (directMatch is not null)
        {
            return directMatch;
        }

        try
        {
            var foregroundProcess = Process.GetProcessById(foregroundProcessId.Value);
            var foregroundName = foregroundProcess.ProcessName;
            if (string.IsNullOrWhiteSpace(foregroundName))
            {
                return null;
            }

            return TrackedApplications.FirstOrDefault(app =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(app.Path),
                    foregroundName,
                    StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }
}
