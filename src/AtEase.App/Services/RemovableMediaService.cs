using System.IO;

namespace AtEase.App.Services;

public class RemovableMediaService
{
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
}
