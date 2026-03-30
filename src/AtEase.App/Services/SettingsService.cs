using System.Text.Json;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class SettingsService
{
    private const string AppFolderName = "AtEaseWin11";
    private const string SettingsFileName = "settings.json";
    private const int CurrentSchemaVersion = 3;

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
            await SaveSettingsAsync(defaults, raiseSettingsSavedEvent: false, cancellationToken);
            return defaults;
        }

        ProfileSettings? loaded;
        await using (var stream = File.OpenRead(_settingsFilePath))
        {
            loaded = await JsonSerializer.DeserializeAsync<ProfileSettings>(stream, SerializerOptions, cancellationToken);
        }

        if (loaded is null)
        {
            var defaults = CreateDefaultSettings();
            await SaveSettingsAsync(defaults, raiseSettingsSavedEvent: false, cancellationToken);
            return defaults;
        }

        var (normalized, wasUpdated) = NormalizeSettings(loaded);
        if (wasUpdated)
        {
            await SaveSettingsAsync(normalized, raiseSettingsSavedEvent: false, cancellationToken);
        }

        return normalized;
    }

    public async Task SaveSettingsAsync(ProfileSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveSettingsAsync(settings, raiseSettingsSavedEvent: true, cancellationToken);
    }

    public async Task SaveSelectedLauncherSectionAsync(string section, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken);
        var normalizedSection = NormalizeLauncherSection(section);

        if (string.Equals(settings.SelectedLauncherSection, normalizedSection, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        settings.SelectedLauncherSection = normalizedSection;
        await SaveSettingsAsync(settings, raiseSettingsSavedEvent: false, cancellationToken);
    }

    public async Task SaveSettingsSilentlyAsync(ProfileSettings settings, CancellationToken cancellationToken = default)
    {
        await SaveSettingsAsync(settings, raiseSettingsSavedEvent: false, cancellationToken);
    }

    private async Task SaveSettingsAsync(
        ProfileSettings settings,
        bool raiseSettingsSavedEvent,
        CancellationToken cancellationToken = default)
    {
        EnsureSettingsFolderExists();

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        var tempPath = _settingsFilePath + ".tmp";

        var lastError = default(Exception);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await File.WriteAllTextAsync(tempPath, json, cancellationToken);
                File.Copy(tempPath, _settingsFilePath, overwrite: true);
                File.Delete(tempPath);
                lastError = null;
                break;
            }
            catch (IOException ex)
            {
                lastError = ex;
                await Task.Delay(150 * (attempt + 1), cancellationToken);
            }
            catch (UnauthorizedAccessException ex)
            {
                lastError = ex;
                await Task.Delay(150 * (attempt + 1), cancellationToken);
            }
        }

        if (lastError is not null)
        {
            throw lastError;
        }

        if (raiseSettingsSavedEvent)
        {
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }

    public ProfileSettings CreateDefaultSettings()
    {
        return new ProfileSettings
        {
            SchemaVersion = CurrentSchemaVersion,
            ProfileName = Environment.UserName,
            ShowFolders = true,
            DefaultPageId = "main",
            SelectedLauncherSection = ProfileSettings.LauncherSectionApps,
            IconSizeMode = "Classic",
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

        var normalizedSection = NormalizeLauncherSection(settings.SelectedLauncherSection);
        if (!string.Equals(settings.SelectedLauncherSection, normalizedSection, StringComparison.Ordinal))
        {
            settings.SelectedLauncherSection = normalizedSection;
            wasUpdated = true;
        }

        var normalizedIconSizeMode = NormalizeIconSizeMode(settings.IconSizeMode);
        if (!string.Equals(settings.IconSizeMode, normalizedIconSizeMode, StringComparison.Ordinal))
        {
            settings.IconSizeMode = normalizedIconSizeMode;
            wasUpdated = true;
        }

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

        // Migrate older settings files that predate complete tile persistence.
        if (settings.SchemaVersion < CurrentSchemaVersion)
        {
            if (settings.AppTiles.Count == 0)
            {
                settings.AppTiles = CreateDefaultAppTiles();
                wasUpdated = true;
            }

            if (settings.FolderTiles.Count == 0)
            {
                settings.FolderTiles = CreateDefaultFolderTiles();
                wasUpdated = true;
            }
        }

        if (settings.SchemaVersion != CurrentSchemaVersion)
        {
            settings.SchemaVersion = CurrentSchemaVersion;
            wasUpdated = true;
        }

        return (settings, wasUpdated);
    }

    private static string NormalizeLauncherSection(string? section)
    {
        if (!string.IsNullOrWhiteSpace(section) &&
            section.StartsWith(ProfileSettings.LauncherSectionMediaPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return section;
        }

        if (string.Equals(section, ProfileSettings.LauncherSectionFolders, StringComparison.OrdinalIgnoreCase))
        {
            return ProfileSettings.LauncherSectionFolders;
        }

        return ProfileSettings.LauncherSectionApps;
    }

    private static string NormalizeIconSizeMode(string? mode)
    {
        if (string.Equals(mode, "Large", StringComparison.OrdinalIgnoreCase))
        {
            return "Large";
        }

        return "Classic";
    }
}
