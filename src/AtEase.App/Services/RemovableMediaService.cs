using System.IO;
using System.Management;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class RemovableMediaService
{
    private readonly HashSet<string> _usbLogicalDriveRoots;

    public RemovableMediaService()
    {
        _usbLogicalDriveRoots = GetUsbLogicalDriveRoots();
    }

    public IReadOnlyList<FolderItem> GetRemovableDriveFolderItems()
    {
        var items = new List<FolderItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!IsRemovableLikeDrive(drive))
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
            if (!IsRemovableLikeDrive(drive))
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
            if (!IsRemovableLikeDrive(drive))
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

            var options = new System.IO.EnumerationOptions
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

    private bool IsRemovableLikeDrive(DriveInfo drive)
    {
        if (drive.DriveType == DriveType.Removable || drive.DriveType == DriveType.CDRom)
        {
            return true;
        }

        if (drive.DriveType != DriveType.Fixed)
        {
            return false;
        }

        return _usbLogicalDriveRoots.Contains(drive.Name.TrimEnd('\\').ToUpperInvariant());
    }

    private static HashSet<string> GetUsbLogicalDriveRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var diskSearcher = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_DiskDrive WHERE InterfaceType='USB'");

            foreach (var diskObj in diskSearcher.Get())
            {
                using var disk = (ManagementObject)diskObj;
                var diskId = Convert.ToString(disk["DeviceID"]);
                if (string.IsNullOrWhiteSpace(diskId))
                {
                    continue;
                }

                var escapedDiskId = diskId.Replace("\\", "\\\\");
                using var partitionSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID=\"{escapedDiskId}\"}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                foreach (var partitionObj in partitionSearcher.Get())
                {
                    using var partition = (ManagementObject)partitionObj;
                    var partitionId = Convert.ToString(partition["DeviceID"]);
                    if (string.IsNullOrWhiteSpace(partitionId))
                    {
                        continue;
                    }

                    var escapedPartitionId = partitionId.Replace("\\", "\\\\");
                    using var logicalSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID=\"{escapedPartitionId}\"}} WHERE AssocClass = Win32_LogicalDiskToPartition");

                    foreach (var logicalObj in logicalSearcher.Get())
                    {
                        using var logical = (ManagementObject)logicalObj;
                        var deviceId = Convert.ToString(logical["DeviceID"]);
                        if (string.IsNullOrWhiteSpace(deviceId))
                        {
                            continue;
                        }

                        roots.Add(deviceId.TrimEnd('\\').ToUpperInvariant());
                    }
                }
            }
        }
        catch
        {
            // Ignore WMI discovery errors and fall back to classic removable/CD detection only.
        }

        return roots;
    }
}
