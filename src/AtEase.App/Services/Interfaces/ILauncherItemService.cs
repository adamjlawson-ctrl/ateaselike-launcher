using AtEase.App.Models;

namespace AtEase.App.Services.Interfaces;

public interface ILauncherItemService
{
    IReadOnlyList<LauncherItem> BuildVisibleItems(AppSettings settings);
}
