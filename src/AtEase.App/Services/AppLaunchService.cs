using System.Diagnostics;
using AtEase.App.Models;

namespace AtEase.App.Services;

public class AppLaunchService
{
    public ActionResult Launch(AppItem? app)
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
            var info = new ProcessStartInfo
            {
                FileName = app.Path,
                Arguments = app.Arguments,
                UseShellExecute = true
            };

            var process = Process.Start(info);
            if (process is null)
            {
                return ActionResult.Failure($"Could not open {app.DisplayName}.");
            }

            return ActionResult.Success($"Opened {app.DisplayName}.");
        }
        catch
        {
            return ActionResult.Failure($"Could not open {app.DisplayName}. Check the configured path.");
        }
    }
}