using System.Text.Json;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class SettingsService
{
    private const string AppFolderName = "AtEaseWin11";
    private const string SettingsFileName = "settings.json";
    private const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsFilePath;

    public event EventHandler? SettingsSaved;

    public SettingsService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

        _settingsFilePath = Path.Combine(root, SettingsFileName);
    }

    public async Task<ProfileSettings> LoadSettingsAsync(CancellationToken cancellationToken = default)
    {
        EnsureSettingsFolderExists();

        if (!File.Exists(_settingsFilePath))
        {
            var defaults = CreateDefaultSettings();
            await SaveSettingsAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(_settingsFilePath);
        var loaded = await JsonSerializer.DeserializeAsync<ProfileSettings>(stream, SerializerOptions, cancellationToken);

        if (loaded is null)
        {
            var defaults = CreateDefaultSettings();
            await SaveSettingsAsync(defaults, cancellationToken);
            return defaults;
        }

        var (normalized, wasUpdated) = NormalizeSettings(loaded);
        if (wasUpdated)
        {
            await SaveSettingsAsync(normalized, cancellationToken);
        }

        return normalized;
    }

    public async Task SaveSettingsAsync(ProfileSettings settings, CancellationToken cancellationToken = default)
    {
        EnsureSettingsFolderExists();

        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    public ProfileSettings CreateDefaultSettings()
    {
        return new ProfileSettings
        {
            SchemaVersion = CurrentSchemaVersion,
            ProfileName = Environment.UserName,
            ShowFolders = true,
            DefaultPageId = "main",
            AppTiles = CreateDefaultAppTiles(),
            FolderTiles = CreateDefaultFolderTiles()
        };
    }

    public string GetSettingsFilePath()
    {
        return _settingsFilePath;
    }

    private void EnsureSettingsFolderExists()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath)!;
        Directory.CreateDirectory(directory);
    }

    private static List<AppItem> CreateDefaultAppTiles()
    {
        var systemDir = Environment.SystemDirectory;

        return
        [
            new AppItem
            {
                DisplayName = "Calculator",
                Path = Path.Combine(systemDir, "calc.exe"),
                IconHint = "Calculator",
                SortOrder = 0,
                IsVisible = true
            },
            new AppItem
            {
                DisplayName = "Notepad",
                Path = Path.Combine(systemDir, "notepad.exe"),
                IconHint = "Document",
                SortOrder = 1,
                IsVisible = true
            }
        ];
    }

    private static List<FolderItem> CreateDefaultFolderTiles()
    {
        return
        [
            new FolderItem
            {
                DisplayName = "Desktop",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                IconHint = "Folder",
                SortOrder = 0,
                IsVisible = true
            },
            new FolderItem
            {
                DisplayName = "Documents",
                Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                IconHint = "Folder",
                SortOrder = 1,
                IsVisible = true
            }
        ];
    }

    private static (ProfileSettings Settings, bool WasUpdated) NormalizeSettings(ProfileSettings settings)
    {
        var wasUpdated = false;

        settings.ProfileName = string.IsNullOrWhiteSpace(settings.ProfileName)
            ? Environment.UserName
            : settings.ProfileName;

        if (settings.AppTiles is null)
        {
            settings.AppTiles = [];
            wasUpdated = true;
        }

        if (settings.FolderTiles is null)
        {
            settings.FolderTiles = [];
            wasUpdated = true;
        }

        // Migrate older settings files that predate persisted tile lists.
        if (settings.SchemaVersion < CurrentSchemaVersion && settings.AppTiles.Count == 0 && settings.FolderTiles.Count == 0)
        {
            settings.AppTiles = CreateDefaultAppTiles();
            settings.FolderTiles = CreateDefaultFolderTiles();
            wasUpdated = true;
        }

        if (settings.SchemaVersion != CurrentSchemaVersion)
        {
            settings.SchemaVersion = CurrentSchemaVersion;
            wasUpdated = true;
        }

        return (settings, wasUpdated);
    }
}
