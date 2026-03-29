using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class IconService : IIconService
{
    public string GetIconHint(string itemPath)
    {
        if (Directory.Exists(itemPath))
        {
            return "Folder";
        }

        var extension = Path.GetExtension(itemPath);
        return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ? "App" : "File";
    }
}
