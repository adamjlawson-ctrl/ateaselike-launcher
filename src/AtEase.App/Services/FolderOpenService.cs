using System.Diagnostics;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class FolderOpenService
{
    public ActionResult Open(FolderItem? folder)
    {
        if (folder is null)
        {
            return ActionResult.Failure("The selected folder is not available.");
        }

        if (string.IsNullOrWhiteSpace(folder.Path))
        {
            return ActionResult.Failure($"{folder.DisplayName} is missing a valid folder path.");
        }

        if (!Directory.Exists(folder.Path))
        {
            return ActionResult.Failure($"{folder.DisplayName} cannot be opened because the folder was not found.");
        }

        try
        {
            var info = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder.Path}\"",
                UseShellExecute = true
            };

            var process = Process.Start(info);
            if (process is null)
            {
                return ActionResult.Failure($"Could not open {folder.DisplayName}.");
            }

            return ActionResult.Success($"Opened {folder.DisplayName}.");
        }
        catch
        {
            return ActionResult.Failure($"Could not open {folder.DisplayName}. Check the configured path.");
        }
    }
}