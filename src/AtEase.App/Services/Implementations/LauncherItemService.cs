using AtEase.App.Models;
using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class LauncherItemService : ILauncherItemService
{
    public IReadOnlyList<LauncherItem> BuildVisibleItems(AppSettings settings)
    {
        var items = settings.Apps
            .Cast<LauncherItem>()
            .Concat(settings.Folders)
            .Where(item => item.IsVisible)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DisplayName)
            .ToList();

        return items;
    }
}
