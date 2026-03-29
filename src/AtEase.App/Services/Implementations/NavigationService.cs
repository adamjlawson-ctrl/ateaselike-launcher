using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class NavigationService : INavigationService
{
    public string CurrentPageKey { get; private set; } = "Home";

    public event EventHandler? PageChanged;

    public void NavigateTo(string pageKey)
    {
        if (string.Equals(CurrentPageKey, pageKey, StringComparison.Ordinal))
        {
            return;
        }

        CurrentPageKey = pageKey;
        PageChanged?.Invoke(this, EventArgs.Empty);
    }
}
