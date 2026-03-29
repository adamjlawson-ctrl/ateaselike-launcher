using System.IO;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class RemovableMediaService
{
    public IReadOnlyList<FolderItem> GetRemovableDriveFolderItems()
    {
        var items = new List<FolderItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable)
            {
                continue;
            }

            if (!drive.IsReady)
            {
                continue;
            }

            var rootPath = drive.Name;
            if (!Directory.Exists(rootPath))
            {
                continue;
            }

            if (!IsAccessible(rootPath))
            {
                continue;
            }

            var label = BuildRemovableDriveLabel(drive);
            items.Add(new FolderItem
            {
                DisplayName = label,
                Path = rootPath,
                IconHint = "RemovableDrive",
                IsVisible = true,
                SortOrder = int.MaxValue
            });
        }

        return items
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<RemovableMediaPanel> GetRemovableMediaPanels()
    {
        var panels = new List<RemovableMediaPanel>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.CDRom)
            {
                continue;
            }

            var rootPath = drive.Name;
            var id = $"{ProfileSettings.LauncherSectionMediaPrefix}{rootPath.ToLowerInvariant()}";
            var title = BuildDriveTitle(drive);

            panels.Add(new RemovableMediaPanel
            {
                Id = id,
                Title = title,
                RootPath = rootPath,
                CurrentPath = rootPath
            });
        }

        return panels
            .OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetRemovableMediaDisplayNames()
    {
        var names = new List<string>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.CDRom)
            {
                continue;
            }

            var label = drive.Name;
            if (drive.IsReady)
            {
                var volumeLabel = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Media" : drive.VolumeLabel;
                label = $"{volumeLabel} ({drive.Name.TrimEnd('\\')})";
            }

            names.Add(label);
        }

        return names;
    }

    public IReadOnlyList<MediaPanelItem> GetPanelItems(string rootPath, string currentPath)
    {
        var items = new List<MediaPanelItem>();

        if (string.IsNullOrWhiteSpace(currentPath) || !Directory.Exists(currentPath))
        {
            return items;
        }

        try
        {
            var currentDirectory = new DirectoryInfo(currentPath);
            var rootDirectory = new DirectoryInfo(rootPath);

            if (!string.Equals(currentDirectory.FullName.TrimEnd('\\'), rootDirectory.FullName.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
                && currentDirectory.Parent is not null)
            {
                items.Add(new MediaPanelItem
                {
                    DisplayName = "..",
                    Path = currentDirectory.Parent.FullName,
                    IsFolder = true,
                    IsParentNavigation = true,
                    IconHint = string.Empty
                });
            }

            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
                ReturnSpecialDirectories = false
            };

            foreach (var directoryPath in Directory.EnumerateDirectories(currentPath, "*", options)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(directoryPath);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = directoryPath;
                }

                items.Add(new MediaPanelItem
                {
                    DisplayName = name,
                    Path = directoryPath,
                    IsFolder = true,
                    IconHint = string.Empty
                });
            }

            foreach (var filePath in Directory.EnumerateFiles(currentPath, "*", options)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = filePath;
                }

                items.Add(new MediaPanelItem
                {
                    DisplayName = name,
                    Path = filePath,
                    IsFolder = false,
                    IconHint = GetImageIconHint(new FileInfo(filePath))
                });
            }
        }
        catch
        {
            return [];
        }

        return items;
    }

    private static string BuildDriveTitle(DriveInfo drive)
    {
        if (!drive.IsReady)
        {
            return drive.Name.TrimEnd('\\');
        }

        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel)
            ? drive.Name.TrimEnd('\\')
            : drive.VolumeLabel;

        return $"{label} ({drive.Name.TrimEnd('\\')})";
    }

    private static string BuildRemovableDriveLabel(DriveInfo drive)
    {
        var driveLetter = drive.Name.TrimEnd('\\');
        if (!string.IsNullOrWhiteSpace(drive.VolumeLabel))
        {
            return $"{drive.VolumeLabel} ({driveLetter})";
        }

        return $"USB Drive ({driveLetter})";
    }

    private static bool IsAccessible(string rootPath)
    {
        try
        {
            using var _ = Directory.EnumerateFileSystemEntries(rootPath).GetEnumerator();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetImageIconHint(FileInfo file)
    {
        var extension = file.Extension;
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
        {
            return file.FullName;
        }

        return string.Empty;
    }
}
