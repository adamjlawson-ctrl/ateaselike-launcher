using System.Diagnostics;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class AppLaunchService
{
    private readonly DisplayLayoutService _displayLayoutService;

    public AppLaunchService(DisplayLayoutService displayLayoutService)
    {
        _displayLayoutService = displayLayoutService;
    }

    public event Action<TrackedApplication>? ApplicationLaunched;

    public ActionResult Launch(AppItem? app, DisplayTarget? displayTarget = null)
    {
        if (app is null)
        {
            return ActionResult.Failure("The selected app is not available.");
        }

        if (string.IsNullOrWhiteSpace(app.Path))
        {
            return ActionResult.Failure($"{app.DisplayName} is missing a valid app path.");
        }

        if (!File.Exists(app.Path))
        {
            return ActionResult.Failure($"{app.DisplayName} cannot be opened because the file was not found.");
        }

        try
        {
            var launchUtc = DateTime.UtcNow;
            var preLaunchProcessIds = _displayLayoutService.SnapshotProcessIdsForExecutable(app.Path);
            var preLaunchWindowHandles = _displayLayoutService.SnapshotTopLevelWindowHandles();
            var workingDirectory = Path.GetDirectoryName(app.Path);
            var info = new ProcessStartInfo
            {
                FileName = app.Path,
                Arguments = app.Arguments,
                UseShellExecute = true,
                WindowStyle = displayTarget is null ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Normal,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory
            };

            var process = Process.Start(info);
            if (process is null)
            {
                return ActionResult.Failure($"Could not open {app.DisplayName}.");
            }

            ApplicationLaunched?.Invoke(new TrackedApplication
            {
                DisplayName = app.DisplayName,
                Path = app.Path,
                ProcessId = process.Id
            });

            if (displayTarget is not null)
            {
                Debug.WriteLine($"[AtEase] Display target selected: {displayTarget.Label} primary={displayTarget.IsPrimary} bounds=({displayTarget.Left},{displayTarget.Top},{displayTarget.Width},{displayTarget.Height})");
                var moved = _displayLayoutService.TryMoveAndMaximizeProcessWindow(
                    process.Id,
                    app.Path,
                    preLaunchProcessIds,
                    preLaunchWindowHandles,
                    launchUtc,
                    displayTarget);
                if (!moved)
                {
                    return ActionResult.Success($"Opened {app.DisplayName}. Could not place window on {displayTarget.Label}.");
                }
            }

            return ActionResult.Success($"Opened {app.DisplayName}.");
        }
        catch
        {
            return ActionResult.Failure($"Could not open {app.DisplayName}. Check the configured path.");
        }
    }
}