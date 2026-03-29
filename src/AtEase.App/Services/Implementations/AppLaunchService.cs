using System.Diagnostics;
using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class AppLaunchService : IAppLaunchService
{
    public bool TryLaunch(string path, string? arguments = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Arguments = arguments ?? string.Empty
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
