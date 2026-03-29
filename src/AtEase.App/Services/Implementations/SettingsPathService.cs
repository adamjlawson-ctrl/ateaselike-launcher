using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class SettingsPathService : ISettingsPathService
{
    public string GetSettingsFilePath()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtEaseWin11");

        return Path.Combine(root, "settings.json");
    }
}
