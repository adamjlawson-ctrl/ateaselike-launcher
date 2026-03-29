using Microsoft.Win32;

namespace AtEase.App.Services;

public class WallpaperService
{
    private const string DesktopRegistryKey = @"HKEY_CURRENT_USER\Control Panel\Desktop";
    private const string WallpaperValueName = "WallPaper";

    public string? GetWallpaperPath()
    {
        var value = Registry.GetValue(DesktopRegistryKey, WallpaperValueName, null) as string;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return File.Exists(value) ? value : null;
    }
}
