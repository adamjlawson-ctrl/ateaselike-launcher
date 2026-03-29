using System.Text.Json;
using AtEase.App.Models;
using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ISettingsPathService _pathService;
    private readonly IValidationService _validationService;

    public JsonSettingsService(ISettingsPathService pathService, IValidationService validationService)
    {
        _pathService = pathService;
        _validationService = validationService;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = _pathService.GetSettingsFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (!File.Exists(path))
        {
            var defaults = CreateDefaultSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(path);
        var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions, cancellationToken);

        if (loaded is null)
        {
            var defaults = CreateDefaultSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        var errors = _validationService.ValidateSettings(loaded);
        if (errors.Count > 0)
        {
            var defaults = CreateDefaultSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        return loaded;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var errors = _validationService.ValidateSettings(settings);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        var path = _pathService.GetSettingsFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions, cancellationToken);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var calcPath = Path.Combine(Environment.SystemDirectory, "calc.exe");
        var notepadPath = Path.Combine(Environment.SystemDirectory, "notepad.exe");

        var apps = new List<AppItem>
        {
            new()
            {
                DisplayName = "Calculator",
                Path = calcPath,
                SortOrder = 0,
                IsVisible = true,
                IconHint = "Calculator"
            },
            new()
            {
                DisplayName = "Notepad",
                Path = notepadPath,
                SortOrder = 1,
                IsVisible = true,
                IconHint = "Document"
            }
        };

        var folders = new List<FolderItem>
        {
            new()
            {
                DisplayName = "Desktop",
                Path = desktopPath,
                SortOrder = 2,
                IsVisible = true,
                IconHint = "Desktop"
            },
            new()
            {
                DisplayName = "Documents",
                Path = docsPath,
                SortOrder = 3,
                IsVisible = true,
                IconHint = "Folder"
            }
        };

        var page = new LauncherPage
        {
            Title = "Home",
            ItemIds = apps.Select(a => a.Id).Concat(folders.Select(f => f.Id)).ToList()
        };

        return new AppSettings
        {
            Version = 1,
            Apps = apps,
            Folders = folders,
            Pages = [page],
            DesktopBrowsing = new DesktopBrowsingSettings
            {
                IsEnabled = false,
                RootFolder = desktopPath,
                ShowHiddenItems = false
            }
        };
    }
}
