namespace AtEase.App.Services.Interfaces;

public interface INavigationService
{
    string CurrentPageKey { get; }

    event EventHandler? PageChanged;

    void NavigateTo(string pageKey);
}
