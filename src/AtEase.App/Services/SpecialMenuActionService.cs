using System.Diagnostics;
using System.IO;

namespace AtEase.App.Services;

public class SpecialMenuActionService
{
    private readonly RemovableMediaService _removableMediaService;

    public SpecialMenuActionService(RemovableMediaService removableMediaService)
    {
        _removableMediaService = removableMediaService;
    }

    public ActionResult Sleep()
    {
        if (!ArePowerActionsEnabled())
        {
            return ActionResult.Failure("Sleep is disabled by default for safety. Set ATEASE_ENABLE_POWER_ACTIONS=1 to enable.");
        }

        return TryStartProcess("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0", "Entering sleep mode.", "Could not start sleep action.");
    }

    public ActionResult ShutDown()
    {
        if (!ArePowerActionsEnabled())
        {
            return ActionResult.Failure("Shut Down is disabled by default for safety. Set ATEASE_ENABLE_POWER_ACTIONS=1 to enable.");
        }

        return TryStartProcess("shutdown.exe", "/s /t 0", "Shutting down Windows.", "Could not start shut down action.");
    }

    public ActionResult ExitAtEase()
    {
        return ActionResult.Success("Exiting At Ease.");
    }

    public ActionResult EjectRemovableMedia()
    {
        var devices = _removableMediaService.GetRemovableMediaDisplayNames();
        if (devices.Count == 0)
        {
            return ActionResult.Failure("No removable media found.");
        }

        var dialogResult = TryStartProcess(
            "RunDll32.exe",
            "shell32.dll,Control_RunDLL hotplug.dll",
            "Opened removable media dialog.",
            "Removable media detected, but could not open eject dialog.");

        if (dialogResult.IsSuccess)
        {
            return ActionResult.Success($"{dialogResult.Message} Devices: {string.Join(", ", devices)}");
        }

        return ActionResult.Failure($"{dialogResult.Message} Devices: {string.Join(", ", devices)}");
    }

    public ActionResult EjectRemovableMedia(string driveRootPath, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(driveRootPath))
        {
            return ActionResult.Failure("No removable drive selected.");
        }

        var normalizedRoot = NormalizeDriveRootPath(driveRootPath);
        if (normalizedRoot is null)
        {
            return ActionResult.Failure("Invalid removable drive path.");
        }

        var drive = DriveInfo.GetDrives()
            .FirstOrDefault(d =>
                (d.DriveType == DriveType.Removable || d.DriveType == DriveType.CDRom)
                && string.Equals(d.Name, normalizedRoot, StringComparison.OrdinalIgnoreCase));

        if (drive is null)
        {
            return ActionResult.Failure("Selected removable drive is no longer available.");
        }

        var title = string.IsNullOrWhiteSpace(displayName)
            ? normalizedRoot.TrimEnd('\\')
            : displayName;

        if (TryEjectDriveWithShellVerb(normalizedRoot))
        {
            return ActionResult.Success($"Eject requested for {title}.");
        }

        var dialogResult = TryStartProcess(
            "RunDll32.exe",
            "shell32.dll,Control_RunDLL hotplug.dll",
            $"Could not directly eject {title}. Opened removable media dialog.",
            $"Could not eject {title}. Try removing it from Windows safely remove dialog.");

        return dialogResult;
    }

    private static string? NormalizeDriveRootPath(string driveRootPath)
    {
        var trimmed = driveRootPath.Trim();
        if (trimmed.Length < 2 || trimmed[1] != ':')
        {
            return null;
        }

        var letter = char.ToUpperInvariant(trimmed[0]);
        if (!char.IsLetter(letter))
        {
            return null;
        }

        return $"{letter}:\\";
    }

    private static bool TryEjectDriveWithShellVerb(string normalizedRoot)
    {
        try
        {
            var driveName = normalizedRoot.TrimEnd('\\');
            var escapedDriveName = driveName.Replace("'", "''", StringComparison.Ordinal);
            var script =
                $"$shell=New-Object -ComObject Shell.Application;" +
                $"$item=$shell.Namespace(17).ParseName('{escapedDriveName}');" +
                "$item.InvokeVerb('Eject')";

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(3000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool ArePowerActionsEnabled()
    {
        var value = Environment.GetEnvironmentVariable("ATEASE_ENABLE_POWER_ACTIONS") ?? string.Empty;
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static ActionResult TryStartProcess(string fileName, string arguments, string successMessage, string failureMessage)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true
            });

            return ActionResult.Success(successMessage);
        }
        catch
        {
            return ActionResult.Failure(failureMessage);
        }
    }
}
