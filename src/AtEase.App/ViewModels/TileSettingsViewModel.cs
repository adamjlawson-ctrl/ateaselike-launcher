using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AtEase.App.Models;
using AtEase.App.Services;
using CommunityToolkit.Mvvm.Input;

namespace AtEase.App.ViewModels;

public partial class TileSettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly PathPickerService _pathPickerService;
    private readonly AppPickerService _appPickerService;
    private ProfileSettings _settings = new();
    private string _statusMessage = "Edit tiles and select Save Changes.";
    private string _selectedIconSizeMode = "Classic";
    private string? _pendingAppRemovalId;
    private string? _pendingFolderRemovalId;

    public ObservableCollection<AppItem> AppTiles { get; } = [];

    public ObservableCollection<FolderItem> FolderTiles { get; } = [];

    public ObservableCollection<string> IconSizeOptions { get; } = ["Classic", "Large"];

    public string SelectedIconSizeMode
    {
        get => _selectedIconSizeMode;
        set => SetProperty(ref _selectedIconSizeMode, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void SetStatusMessageForDiagnostics(string message)
    {
        StatusMessage = message;
    }

    public IRelayCommand AddAppTileCommand { get; }

    public IRelayCommand AddFolderTileCommand { get; }

    public IRelayCommand<AppItem> RemoveAppTileCommand { get; }

    public IRelayCommand<FolderItem> RemoveFolderTileCommand { get; }

    public IAsyncRelayCommand<AppItem> BrowseAppPathCommand { get; }

    public IAsyncRelayCommand<FolderItem> BrowseFolderPathCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public TileSettingsViewModel(
        SettingsService settingsService,
        PathPickerService pathPickerService,
        AppPickerService appPickerService)
    {
        _settingsService = settingsService;
        _pathPickerService = pathPickerService;
        _appPickerService = appPickerService;

        AppTiles.CollectionChanged += AppTiles_CollectionChanged;
        FolderTiles.CollectionChanged += FolderTiles_CollectionChanged;

        AddAppTileCommand = new RelayCommand(AddAppTile);
        AddFolderTileCommand = new RelayCommand(AddFolderTile);
        RemoveAppTileCommand = new RelayCommand<AppItem>(RemoveAppTile);
        RemoveFolderTileCommand = new RelayCommand<FolderItem>(RemoveFolderTile);
        BrowseAppPathCommand = new AsyncRelayCommand<AppItem>(BrowseAppPathAsync);
        BrowseFolderPathCommand = new AsyncRelayCommand<FolderItem>(BrowseFolderPathAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _settings = await _settingsService.LoadSettingsAsync();

        AppTiles.Clear();
        foreach (var app in _settings.AppTiles.OrderBy(a => a.SortOrder))
        {
            AppTiles.Add(CloneApp(app));
        }

        FolderTiles.Clear();
        foreach (var folder in _settings.FolderTiles.OrderBy(f => f.SortOrder))
        {
            FolderTiles.Add(CloneFolder(folder));
        }

        SelectedIconSizeMode = string.Equals(_settings.IconSizeMode, "Large", StringComparison.OrdinalIgnoreCase)
            ? "Large"
            : "Classic";

        RefreshPathHints();
    }

    private void AddAppTile()
    {
        ClearPendingRemovalState();
        var newItem = new AppItem
        {
            DisplayName = "New App",
            Path = string.Empty,
            IsVisible = true,
            SortOrder = NextAppSortOrder()
        };

        AppTiles.Add(newItem);
        RefreshPathHints();
    }

    private void AddFolderTile()
    {
        ClearPendingRemovalState();
        var newItem = new FolderItem
        {
            DisplayName = "New Folder",
            Path = string.Empty,
            IsVisible = true,
            SortOrder = NextFolderSortOrder()
        };

        FolderTiles.Add(newItem);
        RefreshPathHints();
    }

    private void RemoveAppTile(AppItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (_pendingAppRemovalId != item.Id)
        {
            _pendingAppRemovalId = item.Id;
            _pendingFolderRemovalId = null;
            StatusMessage = $"Select Remove again to confirm deleting app tile '{item.DisplayName}'.";
            return;
        }

        AppTiles.Remove(item);
        _pendingAppRemovalId = null;
        StatusMessage = $"Removed app tile '{item.DisplayName}'.";
        RefreshPathHints();
    }

    private void RemoveFolderTile(FolderItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (_pendingFolderRemovalId != item.Id)
        {
            _pendingFolderRemovalId = item.Id;
            _pendingAppRemovalId = null;
            StatusMessage = $"Select Remove again to confirm deleting folder tile '{item.DisplayName}'.";
            return;
        }

        FolderTiles.Remove(item);
        _pendingFolderRemovalId = null;
        StatusMessage = $"Removed folder tile '{item.DisplayName}'.";
        RefreshPathHints();
    }

    private async Task BrowseAppPathAsync(AppItem? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            StatusMessage = "Opening app picker...";

            var excludedPaths = AppTiles
                .Where(app => app.Id != item.Id)
                .Select(app => app.Path)
                .ToList();

            var result = await _appPickerService.PickAppAsync(
                excludedPaths,
                diagnostics => SetStatusMessageForDiagnostics(diagnostics));
            if (result.IsCancelled)
            {
                StatusMessage = "App selection cancelled.";
                return;
            }

            if (!result.IsSuccess)
            {
                StatusMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "App picker failed."
                    : result.Message;
                return;
            }

            if (result.Entry is null)
            {
                StatusMessage = "App selection failed unexpectedly.";
                return;
            }

            item.DisplayName = result.Entry.DisplayName;
            item.Path = result.Entry.LaunchPath;
            if (!string.IsNullOrWhiteSpace(result.Entry.IconHint))
            {
                item.IconHint = result.Entry.IconHint;
            }

            RefreshPathHints();
            StatusMessage = "App selected.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Browse failed: {ex.Message}";
        }
    }

    private async Task BrowseFolderPathAsync(FolderItem? item)
    {
        if (item is null)
        {
            return;
        }

        var result = await _pathPickerService.PickFolderPathAsync();
        if (result.IsCancelled)
        {
            return;
        }

        if (!result.IsSuccess)
        {
            StatusMessage = result.Message;
            return;
        }

        item.Path = result.Path;
        if (string.IsNullOrWhiteSpace(item.DisplayName))
        {
            item.DisplayName = new DirectoryInfo(result.Path).Name;
        }

        RefreshPathHints();
        StatusMessage = "Folder path selected.";
    }

    private async Task SaveAsync()
    {
        ClearPendingRemovalState();
        SanitizeTiles();

        if (!ValidateEntries(out var validationError))
        {
            StatusMessage = validationError;
            return;
        }

        var appPathDuplicates = FindDuplicatePaths(AppTiles.Select(a => a.Path));
        var folderPathDuplicates = FindDuplicatePaths(FolderTiles.Select(f => f.Path));

        _settings.AppTiles = AppTiles.OrderBy(a => a.SortOrder).Select(CloneApp).ToList();
        _settings.FolderTiles = FolderTiles.OrderBy(f => f.SortOrder).Select(CloneFolder).ToList();
        _settings.IconSizeMode = string.Equals(SelectedIconSizeMode, "Large", StringComparison.OrdinalIgnoreCase)
            ? "Large"
            : "Classic";

        try
        {
            await _settingsService.SaveSettingsAsync(_settings);

            var warnings = new List<string>();
            if (appPathDuplicates.Count > 0)
            {
                warnings.Add($"Duplicate app paths detected ({appPathDuplicates.Count}).");
            }

            if (folderPathDuplicates.Count > 0)
            {
                warnings.Add($"Duplicate folder paths detected ({folderPathDuplicates.Count}).");
            }

            StatusMessage = warnings.Count == 0
                ? "Settings saved successfully."
                : $"Settings saved with warnings: {string.Join(" ", warnings)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not save settings: {ex.Message}";
        }
    }

    private bool ValidateEntries(out string message)
    {
        if (AppTiles.Any(a => string.IsNullOrWhiteSpace(a.DisplayName) || string.IsNullOrWhiteSpace(a.Path)))
        {
            message = "Each app tile needs a name and path before saving.";
            return false;
        }

        if (FolderTiles.Any(f => string.IsNullOrWhiteSpace(f.DisplayName) || string.IsNullOrWhiteSpace(f.Path)))
        {
            message = "Each folder tile needs a name and path before saving.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void SanitizeTiles()
    {
        TrimAppTileValues();
        TrimFolderTileValues();

        RemoveBlankAppTiles();
        RemoveBlankFolderTiles();
        RefreshPathHints();
    }

    private void TrimAppTileValues()
    {
        foreach (var app in AppTiles)
        {
            app.DisplayName = app.DisplayName.Trim();
            app.Path = app.Path.Trim();
        }
    }

    private void TrimFolderTileValues()
    {
        foreach (var folder in FolderTiles)
        {
            folder.DisplayName = folder.DisplayName.Trim();
            folder.Path = folder.Path.Trim();
        }
    }

    private void RemoveBlankAppTiles()
    {
        var blankApps = AppTiles
            .Where(a => string.IsNullOrWhiteSpace(a.DisplayName) && string.IsNullOrWhiteSpace(a.Path))
            .ToList();

        foreach (var app in blankApps)
        {
            AppTiles.Remove(app);
        }
    }

    private void RemoveBlankFolderTiles()
    {
        var blankFolders = FolderTiles
            .Where(f => string.IsNullOrWhiteSpace(f.DisplayName) && string.IsNullOrWhiteSpace(f.Path))
            .ToList();

        foreach (var folder in blankFolders)
        {
            FolderTiles.Remove(folder);
        }
    }

    private static List<string> FindDuplicatePaths(IEnumerable<string> paths)
    {
        return paths
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
    }

    private void AppTiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (AppItem item in e.OldItems)
            {
                item.PropertyChanged -= AppTile_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (AppItem item in e.NewItems)
            {
                item.PropertyChanged += AppTile_PropertyChanged;
            }
        }

        RefreshPathHints();
    }

    private void FolderTiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (FolderItem item in e.OldItems)
            {
                item.PropertyChanged -= FolderTile_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (FolderItem item in e.NewItems)
            {
                item.PropertyChanged += FolderTile_PropertyChanged;
            }
        }

        RefreshPathHints();
    }

    private void AppTile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherItem.Path) || string.IsNullOrEmpty(e.PropertyName))
        {
            RefreshPathHints();
        }
    }

    private void FolderTile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherItem.Path) || string.IsNullOrEmpty(e.PropertyName))
        {
            RefreshPathHints();
        }
    }

    private void RefreshPathHints()
    {
        var duplicateAppPaths = FindDuplicatePathSet(AppTiles.Select(a => a.Path));
        var duplicateFolderPaths = FindDuplicatePathSet(FolderTiles.Select(f => f.Path));

        foreach (var app in AppTiles)
        {
            app.PathHint = BuildPathHint(app.Path, duplicateAppPaths, isFile: true);
        }

        foreach (var folder in FolderTiles)
        {
            folder.PathHint = BuildPathHint(folder.Path, duplicateFolderPaths, isFile: false);
        }
    }

    private static HashSet<string> FindDuplicatePathSet(IEnumerable<string> paths)
    {
        return paths
            .Select(NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .GroupBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildPathHint(string rawPath, HashSet<string> duplicatePaths, bool isFile)
    {
        var normalizedPath = NormalizePath(rawPath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return string.Empty;
        }

        var duplicateHint = duplicatePaths.Contains(normalizedPath)
            ? "Duplicate path in this list."
            : string.Empty;

        var exists = isFile ? File.Exists(normalizedPath) : Directory.Exists(normalizedPath);
        var existenceHint = exists
            ? (isFile ? "File found." : "Folder found.")
            : (isFile ? "File not found yet." : "Folder not found yet.");

        return string.IsNullOrEmpty(duplicateHint)
            ? existenceHint
            : $"{duplicateHint} {existenceHint}";
    }

    private static string NormalizePath(string? path)
    {
        return path?.Trim() ?? string.Empty;
    }

    private void ClearPendingRemovalState()
    {
        _pendingAppRemovalId = null;
        _pendingFolderRemovalId = null;
    }

    private int NextAppSortOrder()
    {
        return AppTiles.Count == 0 ? 0 : AppTiles.Max(a => a.SortOrder) + 1;
    }

    private int NextFolderSortOrder()
    {
        return FolderTiles.Count == 0 ? 0 : FolderTiles.Max(f => f.SortOrder) + 1;
    }

    private static AppItem CloneApp(AppItem item)
    {
        return new AppItem
        {
            Id = item.Id,
            DisplayName = item.DisplayName,
            Path = item.Path,
            IsVisible = item.IsVisible,
            SortOrder = item.SortOrder,
            IconHint = item.IconHint,
            Arguments = item.Arguments
        };
    }

    private static FolderItem CloneFolder(FolderItem item)
    {
        return new FolderItem
        {
            Id = item.Id,
            DisplayName = item.DisplayName,
            Path = item.Path,
            IsVisible = item.IsVisible,
            SortOrder = item.SortOrder,
            IconHint = item.IconHint
        };
    }
}